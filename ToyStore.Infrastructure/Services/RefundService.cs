using AutoMapper;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Application.Services;
using ToyStore.Infrastructure.Options;
using ToyStore.Application.Common.Helpers;
using FluentValidation;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Service triển khai toàn bộ nghiệp vụ xử lý Yêu cầu hoàn tiền / Đổi trả sản phẩm (Refunds),
/// bao gồm cả luồng do Khách hàng tạo (Customer-initiated) và luồng Tự động do Hệ thống tạo khi GHN giao hàng thất bại (System-initiated).
/// </summary>
public class RefundService : IRefundService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IGhnClient _ghnClient;
    private readonly GhnOptions _ghnOptions;
    private readonly ShopAddressOptions _shopAddress;
    private readonly IWalletRefundCreditor _walletRefundCreditor;
    private readonly IShiftAssignmentService _shiftAssignmentService;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<RefundService> _logger;
    private readonly IValidator<CreateRefundDto> _createRefundValidator;
    private readonly IValidator<UpdateRefundStatusDto> _updateRefundStatusValidator;

    /// <summary>
    /// Khởi tạo RefundService với đầy đủ các dependency quản lý dữ liệu, tích hợp GHN, ví tiền, phân ca và xác thực.
    /// </summary>
    public RefundService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IDomainEventPublisher eventPublisher,
        IGhnClient ghnClient,
        IOptions<GhnOptions> ghnOptions,
        IOptions<ShopAddressOptions> shopAddress,
        IWalletRefundCreditor walletRefundCreditor,
        IShiftAssignmentService shiftAssignmentService,
        ITimeProvider timeProvider,
        ILogger<RefundService> logger,
        IValidator<CreateRefundDto> createRefundValidator,
        IValidator<UpdateRefundStatusDto> updateRefundStatusValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
        _ghnClient = ghnClient;
        _ghnOptions = ghnOptions.Value;
        _shopAddress = shopAddress.Value;
        _walletRefundCreditor = walletRefundCreditor;
        _shiftAssignmentService = shiftAssignmentService;
        _timeProvider = timeProvider;
        _logger = logger;
        _createRefundValidator = createRefundValidator;
        _updateRefundStatusValidator = updateRefundStatusValidator;
    }

    /// <summary>
    /// Chuyển đổi tên chuỗi trạng thái (không phân biệt chữ hoa/thường) sang mã định danh byte ID tương ứng trong bảng StatusRefund.
    /// </summary>
    /// <param name="statusStr">Chuỗi tên trạng thái.</param>
    /// <returns>Mã byte ID trạng thái hoặc null nếu không hợp lệ.</returns>
    private byte? MapStatusStringToId(string statusStr)
    {
        if (string.IsNullOrWhiteSpace(statusStr))
            return null;

        statusStr = statusStr.Trim();
        var normalized = statusStr.ToLowerInvariant();
        return normalized switch
        {
            "refundrequested" or "requested" => (byte)RefundStatusEnum.RefundRequested,
            "refundapproved" or "approved" => (byte)RefundStatusEnum.RefundApproved,
            "refundrejected" or "rejected" => (byte)RefundStatusEnum.RefundRejected,
            "refundpickupcreated" => (byte)RefundStatusEnum.RefundPickupCreated,
            "refundshipping" => (byte)RefundStatusEnum.RefundShipping,
            "refundreceived" => (byte)RefundStatusEnum.RefundReceived,
            "refundinspectionpending" => (byte)RefundStatusEnum.RefundInspectionPending,
            "refundcompleted" or "completed" => (byte)RefundStatusEnum.RefundCompleted,
            "refundcancelled" or "cancelled" => (byte)RefundStatusEnum.RefundCancelled,
            "refunddamage" or "damage" => (byte)RefundStatusEnum.RefundDamage,
            "refundreturnshipmentcreated" => (byte)RefundStatusEnum.RefundReturnShipmentCreated,
            "refundreturningtocustomer" => (byte)RefundStatusEnum.RefundReturningToCustomer,
            "refundreturnedtocustomer" => (byte)RefundStatusEnum.RefundReturnedToCustomer,
            "refundreturntocustomerfailed" => (byte)RefundStatusEnum.RefundReturnToCustomerFailed,
            _ => null
        };
    }

    /// <summary>
    /// Tính số tiền thực tế credit vào ví khách hàng sau khi cấn trừ phí ship hoàn trả (nếu khách chịu phí).
    /// Số tiền ApprovedAmount gốc giữ nguyên không đổi.
    /// </summary>
    /// <param name="approvedAmount">Tổng số tiền hoàn duyệt tối đa.</param>
    /// <param name="returnShippingFee">Phí vận chuyển hoàn trả hàng về shop.</param>
    /// <param name="returnShippingFeeBy">Bên chịu phí hoàn ("Store" hoặc "Customer").</param>
    /// <returns>Số tiền thực nhận sau khấu trừ.</returns>
    private static decimal ComputeFinalRefundAmount(decimal approvedAmount, decimal returnShippingFee, string returnShippingFeeBy)
    {
        if (string.Equals(returnShippingFeeBy, RefundResponsibleParty.Customer, StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(0m, approvedAmount - returnShippingFee);
        }
        return approvedAmount;
    }

    /// <summary>
    /// Chuẩn hóa chuỗi trạng thái tìm kiếm từ phía client về đúng định dạng tên trạng thái chuẩn trong cơ sở dữ liệu.
    /// </summary>
    /// <param name="status">Chuỗi trạng thái đầu vào.</param>
    /// <returns>Tên trạng thái chuẩn.</returns>
    private string? NormalizeRefundStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return status;

        var statusId = MapStatusStringToId(status);
        return statusId switch
        {
            (byte)RefundStatusEnum.RefundRequested => RefundStatuses.Requested,
            (byte)RefundStatusEnum.RefundApproved => RefundStatuses.Approved,
            (byte)RefundStatusEnum.RefundRejected => RefundStatuses.Rejected,
            (byte)RefundStatusEnum.RefundPickupCreated => RefundStatuses.PickupCreated,
            (byte)RefundStatusEnum.RefundShipping => RefundStatuses.Shipping,
            (byte)RefundStatusEnum.RefundReceived => RefundStatuses.Received,
            (byte)RefundStatusEnum.RefundInspectionPending => RefundStatuses.InspectionPending,
            (byte)RefundStatusEnum.RefundCompleted => RefundStatuses.Completed,
            (byte)RefundStatusEnum.RefundCancelled => RefundStatuses.Cancelled,
            (byte)RefundStatusEnum.RefundDamage => RefundStatuses.Damage,
            (byte)RefundStatusEnum.RefundReturnShipmentCreated => RefundStatuses.ReturnShipmentCreated,
            (byte)RefundStatusEnum.RefundReturningToCustomer => RefundStatuses.ReturningToCustomer,
            (byte)RefundStatusEnum.RefundReturnedToCustomer => RefundStatuses.ReturnedToCustomer,
            (byte)RefundStatusEnum.RefundReturnToCustomerFailed => RefundStatuses.ReturnToCustomerFailed,
            _ => status
        };
    }

    /// <summary>
    /// Ánh xạ trạng thái nội bộ chi tiết (14 trạng thái backend) sang nhóm trạng thái thân thiện hiển thị cho Khách hàng (Requested, Processing, Completed, Rejected, Cancelled, Damaged).
    /// </summary>
    /// <param name="internalStatus">Tên trạng thái nội bộ.</param>
    /// <returns>Tên trạng thái hiển thị cho khách hàng.</returns>
    private string MapToCustomerFacingStatus(string internalStatus)
    {
        if (string.IsNullOrWhiteSpace(internalStatus))
            return internalStatus;

        return internalStatus switch
        {
            RefundStatuses.Requested => "Requested",
            RefundStatuses.Approved or RefundStatuses.PickupCreated or RefundStatuses.Shipping or RefundStatuses.Received or RefundStatuses.InspectionPending or RefundStatuses.ReturnShipmentCreated or RefundStatuses.ReturningToCustomer => "Processing",
            RefundStatuses.Completed => "Completed",
            RefundStatuses.Rejected or RefundStatuses.ReturnedToCustomer => "Rejected",
            RefundStatuses.Cancelled => "Cancelled",
            RefundStatuses.ReturnToCustomerFailed => RefundStatuses.ReturnToCustomerFailed,
            RefundStatuses.Damage => "Damaged",
            _ => internalStatus
        };
    }

    /// <summary>
    /// Lấy danh sách tất cả các lý do hoàn tiền đang hoạt động (dành cho dropdown lựa chọn khi khách tạo yêu cầu).
    /// </summary>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách DTO lý do hoàn tiền.</returns>
    public async Task<List<RefundReasonDto>> GetRefundReasonsAsync(CancellationToken cancellationToken = default)
    {
        var reasons = await _unitOfWork.Refunds.GetActiveReasonsAsync(cancellationToken);
        return _mapper.Map<List<RefundReasonDto>>(reasons);
    }

    /// <summary>
    /// Khách hàng tạo yêu cầu hoàn tiền / đổi trả cho đơn hàng đã hoàn tất (Completed):
    /// 1. Kiểm tra đơn hàng thuộc quyền sở hữu của khách và đang ở trạng thái Completed.
    /// 2. Kiểm tra thời hạn 3 ngày kể từ ngày đơn hàng hoàn tất (CompletedAt).
    /// 3. Giới hạn tối đa 2 lần yêu cầu hoàn tiền trên 1 đơn hàng.
    /// 4. Kiểm tra xem đơn hàng có yêu cầu hoàn tiền nào khác đang trong tiến trình xử lý chưa.
    /// 5. Phân bổ chiết khấu voucher/khuyến mãi trên từng sản phẩm hoàn trả.
    /// 6. Tính toán tiền hoàn hàng hóa và phí vận chuyển gốc (nếu hoàn toàn bộ đơn và lỗi do phía shop).
    /// 7. Khởi tạo bản ghi OrderRefund, RefundDetail, RefundImage, ghi lịch sử trạng thái ban đầu (RefundRequested).
    /// 8. Tự động phân công đơn hàng cho Nhân viên CSKH (Staff) và Thủ kho (Merchandise) đang trực ca.
    /// 9. Bắn thông báo realtime đến hệ thống nội bộ.
    /// </summary>
    /// <param name="customerId">Mã ID tài khoản khách hàng tạo yêu cầu.</param>
    /// <param name="dto">Dữ liệu yêu cầu hoàn tiền (mã đơn, lý do, danh sách món hàng, hình ảnh bằng chứng).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO chi tiết yêu cầu hoàn tiền vừa tạo.</returns>
    public async Task<Result<RefundDto>> CreateRefundAsync(int customerId, CreateRefundDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra tính hợp lệ của DTO qua FluentValidation
        var validation = await _createRefundValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<RefundDto>();

        // 2. Lấy thông tin đơn hàng và kiểm tra quyền sở hữu của khách hàng
        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(dto.OrderId, cancellationToken);
        if (order == null || order.AccountId != customerId)
            return Result<RefundDto>.NotFound("Order", dto.OrderId);

        // 3. Kiểm tra điều kiện đơn hàng phải ở trạng thái Completed (Đã giao hàng thành công)
        if (order.StatusId != (byte)OrderStatus.Completed)
            return Result<RefundDto>.BusinessError("Order must be in Completed status to request a refund.");

        // 4. Kiểm tra quy tắc thời hạn 3 ngày kể từ thời điểm hoàn tất đơn hàng
        if (order.CompletedAt == null || (DateTime.UtcNow - order.CompletedAt.Value).TotalDays > 3)
            return Result<RefundDto>.BusinessError("Refund requests must be submitted within 3 days of order completion.");

        var existingRefunds = await _unitOfWork.Refunds.GetAdminRefundsAsync(new AdminRefundFilterDto { OrderId = dto.OrderId, PageSize = 100 }, cancellationToken);

        // 5. Giới hạn tối đa 2 lần yêu cầu hoàn tiền cho mỗi đơn hàng
        if (existingRefunds.Items != null && existingRefunds.Items.Count >= 2)
        {
            return Result<RefundDto>.BusinessError("The number of refund requests for this order has exceeded the allowed limit (maximum 2 times)");
        }

        // 6. Kiểm tra xem có yêu cầu hoàn tiền nào trước đó đang được xử lý dở dang không
        var hasBlockedRefund = false;
        foreach (var rDto in existingRefunds.Items)
        {
            var rEntity = await _unitOfWork.Refunds.GetByIdAsync(rDto.RefundId, cancellationToken);
            if (rEntity != null)
            {
                if (rEntity.StatusId != (byte)RefundStatusEnum.RefundCancelled &&
                    rEntity.StatusId != (byte)RefundStatusEnum.RefundRejected &&
                    rEntity.StatusId != (byte)RefundStatusEnum.RefundReturnedToCustomer &&
                    rEntity.StatusId != (byte)RefundStatusEnum.RefundReturnToCustomerFailed)
                {
                    hasBlockedRefund = true;
                    break;
                }

                var wentPastRequested = rEntity.RefundStatusHistories.Any(h =>
                    h.StatusId != (byte)RefundStatusEnum.RefundRequested &&
                    h.StatusId != (byte)RefundStatusEnum.RefundCancelled &&
                    h.StatusId != (byte)RefundStatusEnum.RefundRejected);

                if (wentPastRequested && rEntity.StatusId != (byte)RefundStatusEnum.RefundRejected && rEntity.StatusId != (byte)RefundStatusEnum.RefundReturnedToCustomer)
                {
                    hasBlockedRefund = true;
                    break;
                }
            }
        }

        if (hasBlockedRefund)
        {
            return Result<RefundDto>.BusinessError("This order currently has a refund request being processed.");
        }

        // 7. Xử lý danh sách sản phẩm yêu cầu hoàn trả (hỗ trợ hoàn trả một phần hoặc toàn bộ đơn)
        var returnItems = new List<CreateRefundItemDto>();
        if (dto.Items == null || !dto.Items.Any())
        {
            // Mặc định hoàn trả toàn bộ mặt hàng trong đơn hàng (Full Refund)
            returnItems = order.OrderDetails.Select(od => new CreateRefundItemDto
            {
                ProductId = od.ProductId,
                Quantity = od.Quantity
            }).ToList();
        }
        else
        {
            returnItems = dto.Items;
        }

        // 8. Kiểm tra sản phẩm tồn tại trong đơn hàng và số lượng trả không vượt quá số lượng đã mua
        var refundDetails = new List<RefundDetail>();
        var grossShipping = order.ActualShippingFee ?? order.EstimatedShippingFee;
        var productVoucherDisc = Math.Max(0m, order.VoucherDiscountAmount - grossShipping);
        var discountRatio = order.SubTotal > 0 ? (productVoucherDisc / order.SubTotal) : 0m;

        foreach (var item in returnItems)
        {
            var originalDetail = order.OrderDetails.FirstOrDefault(od => od.ProductId == item.ProductId);
            if (originalDetail == null)
            {
                return Result<RefundDto>.BusinessError($"Product ID {item.ProductId} does not exist in the original order.");
            }

            if (item.Quantity <= 0)
            {
                return Result<RefundDto>.BusinessError($"Returned quantity for Product ID {item.ProductId} must be greater than zero.");
            }

            if (item.Quantity > originalDetail.Quantity)
            {
                return Result<RefundDto>.BusinessError($"Returned quantity ({item.Quantity}) for Product ID {item.ProductId} exceeds purchased quantity ({originalDetail.Quantity}).");
            }

            // Tính đơn giá ròng sau khi phân bổ giảm giá voucher
            var netUnitPrice = originalDetail.Quantity > 0
                ? originalDetail.UnitPrice - (originalDetail.DiscountAmount / originalDetail.Quantity)
                : originalDetail.UnitPrice;
            var itemRefundAmount = Math.Round(item.Quantity * netUnitPrice * (1 - discountRatio), 0);

            refundDetails.Add(new RefundDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = netUnitPrice,
                RefundAmount = itemRefundAmount,
                CreatedAt = DateTime.UtcNow
            });
        }

        // 9. Xác định xem có phải trả toàn bộ mặt hàng trong đơn không
        bool isFullReturn = true;
        foreach (var od in order.OrderDetails)
        {
            var retItem = returnItems.FirstOrDefault(ri => ri.ProductId == od.ProductId);
            if (retItem == null || retItem.Quantity < od.Quantity)
            {
                isFullReturn = false;
                break;
            }
        }

        // Bù trừ sai số làm tròn số lẻ cho đơn hoàn toàn bộ
        if (isFullReturn && refundDetails.Count > 0)
        {
            var targetSubTotal = order.SubTotal - order.VoucherDiscountAmount;
            var currentSubTotal = refundDetails.Sum(d => d.RefundAmount);
            var diff = targetSubTotal - currentSubTotal;
            if (diff != 0)
            {
                refundDetails.Last().RefundAmount += diff;
            }
        }

        // 10. Tra cứu lý do hoàn tiền và bên chịu trách nhiệm
        var activeReasons = await _unitOfWork.Refunds.GetActiveReasonsAsync(cancellationToken);
        var selectedReason = activeReasons.FirstOrDefault(r => r.RefundReasonId == dto.RefundReasonId);
        var isCustomerFault = selectedReason != null && string.Equals(selectedReason.ResponsibleParty, RefundResponsibleParty.Customer, StringComparison.OrdinalIgnoreCase);

        var subTotal = refundDetails.Sum(d => d.RefundAmount);
        // Hoàn phí ship gốc nếu hoàn toàn bộ đơn và không phải lỗi của khách
        var shippingFeeRefunded = (isFullReturn && !isCustomerFault) ? (order.ActualShippingFee ?? order.EstimatedShippingFee) : 0m;
        var totalAmount = subTotal + shippingFeeRefunded;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var refundType = string.Equals(dto.RefundType, RefundTypes.RefundOnly, StringComparison.OrdinalIgnoreCase)
                ? RefundTypes.RefundOnly
                : RefundTypes.ReturnAndRefund;

            // 11. Khởi tạo thực thể OrderRefund
            var refund = new OrderRefund
            {
                OrderId = dto.OrderId,
                RefundReasonId = dto.RefundReasonId,
                ReasonDetails = dto.ReasonDetails,
                RefundType = refundType,
                CustomerId = customerId,
                RequestedBy = customerId,
                ApprovedAmount = totalAmount,
                RefundCode = "REF-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                SubTotal = subTotal,
                ShippingFee = shippingFeeRefunded,
                TotalAmount = totalAmount,
                StatusId = (byte)RefundStatusEnum.RefundRequested,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            // Ghi nhận lịch sử trạng thái đầu tiên: RefundRequested
            refund.RefundStatusHistories.Add(new RefundStatusHistory
            {
                StatusId = (byte)RefundStatusEnum.RefundRequested,
                ChangedBy = customerId,
                Note = "Refund request created by customer.",
                CreatedAt = DateTime.UtcNow
            });

            // Gắn chi tiết mặt hàng hoàn trả
            foreach (var detail in refundDetails)
            {
                refund.RefundDetails.Add(detail);
            }

            await _unitOfWork.Refunds.AddAsync(refund, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken); // Lưu để sinh RefundId

            // Lưu danh sách hình ảnh bằng chứng đính kèm nếu có
            if (dto.Images != null && dto.Images.Any())
            {
                var refundImages = dto.Images.Select(imgUrl => new RefundImage
                {
                    RefundId = refund.RefundId,
                    ImageUrl = imgUrl,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _unitOfWork.RefundImages.AddRangeAsync(refundImages, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 12. Giải phóng phân công cũ và tự động phân công cho nhân viên/thủ kho đang trực ca
            await _shiftAssignmentService.ReleaseCapacityAsync(refund.OrderId, cancellationToken);
            await _shiftAssignmentService.AutoAssignOrderAsync(refund.OrderId, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 13. Phát sự kiện thông báo có yêu cầu hoàn tiền mới
            await _eventPublisher.PublishAsync("Refund", refund.RefundId.ToString(),
                NotificationEventTypes.RefundNewRequest,
                new { refundId = refund.RefundId, orderId = dto.OrderId, orderCode = order.OrderCode, customerId },
                CancellationToken.None);

            var createdRefund = await _unitOfWork.Refunds.GetByIdAsync(refund.RefundId, cancellationToken);
            var dtoResult = _mapper.Map<RefundDto>(createdRefund);
            dtoResult.RefundStatus = MapToCustomerFacingStatus(dtoResult.RefundStatus);
            return Result<RefundDto>.Success(dtoResult);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Hệ thống tự động tạo yêu cầu hoàn trả khi đơn vị vận chuyển GHN giao hàng thất bại (Luồng B - System Initiated):
    /// 1. Kiểm tra đơn hàng chưa có yêu cầu hoàn tiền đang xử lý.
    /// 2. Xác định hình thức hoàn tiền: Đơn COD / Unpaid -> ReturnOnly; Đơn đã thanh toán trước (PAID) -> ReturnAndRefund.
    /// 3. Phân bổ chiết khấu voucher/khuyến mãi trên từng sản phẩm.
    /// 4. Đặt trạng thái ban đầu (mặc định là RefundReceived do hàng đã quay về kho hoặc RefundDamage nếu shipper báo hỏng/mất).
    /// 5. Tạo bản ghi OrderRefund, RefundDetail và ghi lịch sử trạng thái hệ thống.
    /// 6. Tự động kiểm tra và phân công đơn hàng cho nhân viên xử lý nếu chưa có.
    /// </summary>
    /// <param name="order">Thực thể đơn hàng tương ứng.</param>
    /// <param name="refundReasonId">Mã ID lý do hoàn tiền do giao hàng thất bại.</param>
    /// <param name="initialStatusId">Trạng thái khởi tạo tùy chọn (ví dụ: RefundDamage hoặc RefundReceived).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể OrderRefund vừa được tạo hoặc null nếu đã tồn tại.</returns>
    public async Task<OrderRefund?> CreateSystemRefundForDeliveryFailAsync(
        Order order, byte refundReasonId, byte? initialStatusId = null, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra xem đơn hàng đã có yêu cầu hoàn tiền nào đang chạy không
        var existing = await _unitOfWork.Refunds.GetAdminRefundsAsync(
            new AdminRefundFilterDto { OrderId = order.OrderId, PageSize = 10 },
            cancellationToken);

        // Bug Fix #4: Thêm PickupCreated và Shipping vào danh sách kiểm tra
        // để tránh tạo duplicate System Refund khi GHN gửi nhiều webhook liên tiếp (race condition).
        if (existing.Items.Any(r =>
                r.RefundStatus is RefundStatuses.Requested or RefundStatuses.Approved or RefundStatuses.Received
                    or RefundStatuses.InspectionPending or RefundStatuses.Completed or RefundStatuses.Damage
                    or RefundStatuses.PickupCreated or RefundStatuses.Shipping))
        {
            return null;
        }

        var refundOrder = order;
        if (refundOrder.OrderDetails.Count == 0)
        {
            var reloadedOrder = await _unitOfWork.Orders.GetByIdForUpdateAsync(order.OrderId, cancellationToken);
            if (reloadedOrder != null)
            {
                refundOrder = reloadedOrder;
            }
        }

        // 2. Kiểm tra phương thức thanh toán để xác định loại hoàn trả (ReturnOnly đối với COD hoặc ReturnAndRefund với đơn đã trả trước)
        var isUnpaid = refundOrder.PaymentStatus != "PAID"
            || string.Equals(refundOrder.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase);

        var refundType = isUnpaid ? RefundTypes.ReturnOnly : RefundTypes.ReturnAndRefund;

        // Tách discount: chỉ áp discountRatio từ ORDER_TOTAL discount, 
        // không gộp shipping voucher discount vào product subtotal.
        var grossShippingFee = isUnpaid ? 0m : (refundOrder.ActualShippingFee ?? refundOrder.EstimatedShippingFee);
        var orderTotalAmount = isUnpaid ? 0m : refundOrder.TotalAmount;
        var productVoucherDiscount = isUnpaid ? 0m : Math.Max(0m, refundOrder.VoucherDiscountAmount - grossShippingFee);
        var discountRatio = (!isUnpaid && refundOrder.SubTotal > 0)
            ? (productVoucherDiscount / refundOrder.SubTotal)
            : 0m;

        // 3. Phân bổ chi tiết từng mặt hàng và tiền hoàn tương ứng
        var refundDetails = refundOrder.OrderDetails.Select(od =>
        {
            var netUnitPrice = od.Quantity > 0
                ? od.UnitPrice - (od.DiscountAmount / od.Quantity)
                : od.UnitPrice;
            return new RefundDetail
            {
                ProductId = od.ProductId,
                Quantity = od.Quantity,
                UnitPrice = netUnitPrice,
                RefundAmount = isUnpaid ? 0m : Math.Round(od.Quantity * netUnitPrice * (1 - discountRatio), 0),
                RestorableQuantity = od.Quantity, // mặc định = nguyên vẹn; Thủ kho sẽ kiểm tra điều chỉnh khi inspect
                CreatedAt = DateTime.UtcNow
            };
        }).ToList();

        if (!isUnpaid && refundDetails.Count > 0)
        {
            var targetSubTotal = refundOrder.SubTotal - refundOrder.VoucherDiscountAmount;
            var currentSubTotal = refundDetails.Sum(d => d.RefundAmount);
            var diff = targetSubTotal - currentSubTotal;
            if (diff != 0)
            {
                refundDetails.Last().RefundAmount += diff;
            }
        }

        var subTotal = isUnpaid ? 0m : refundDetails.Sum(d => d.RefundAmount);
        var shippingFee = grossShippingFee;
        var customerShippingPaid = isUnpaid ? 0m : Math.Max(0m, orderTotalAmount - subTotal);
        var now = DateTime.UtcNow;

        // Giao hàng thất bại: webhook GHN kích hoạt khi hàng đã về kho -> khởi tạo với trạng thái Received hoặc Damage
        var statusId = initialStatusId ?? (byte)RefundStatusEnum.RefundReceived;

        var rawCancelReason = refundOrder.CancelReason;
        var translatedReason = ToyStore.Infrastructure.Mappers.GhnFailCodeMapper.GetFriendlyDescription(
            refundOrder.LastGHNFailCode,
            !string.IsNullOrWhiteSpace(rawCancelReason) && rawCancelReason != "DELIVERY_FAILED_GHN"
                ? rawCancelReason
                : null);
        var ghnReason = !string.IsNullOrWhiteSpace(translatedReason)
            ? $" (GHN Reason: {translatedReason})"
            : "";
        var ghnCode = !string.IsNullOrWhiteSpace(refundOrder.LastGHNFailCode)
            ? $" (Code: {refundOrder.LastGHNFailCode})"
            : "";

        // Ghi chú nhật ký nội bộ (lưu trong lịch sử, không hiển thị trực tiếp cho khách hàng)
        var internalNote = statusId switch
        {
            (byte)RefundStatusEnum.RefundDamage => $"Auto-created: GHN damaged/lost package in transit{ghnReason}{ghnCode}",
            (byte)RefundStatusEnum.RefundReceived => isUnpaid 
                ? $"Auto-created: GHN COD delivery failure return — pending merchandise inspection{ghnReason}{ghnCode}"
                : $"Auto-created: GHN delivery failure return — goods at shop, pending merchandise inspection{ghnReason}{ghnCode}",
            _ => $"Auto-created: GHN delivery failure return{ghnReason}{ghnCode}"
        };

        // 4. Khởi tạo thực thể OrderRefund hệ thống
        var refund = new OrderRefund
        {
            OrderId = refundOrder.OrderId,
            RefundReasonId = refundReasonId,
            ReasonDetails = null,
            RefundSource = RefundSources.System,   // Luồng B: hệ thống tạo tự động, không có vận đơn pickup từ khách
            RefundType = refundType,
            CustomerId = refundOrder.AccountId,
            RequestedBy = null,
            ApprovedAmount = orderTotalAmount, // Tổng tối đa có thể hoàn (tiền hàng + ship sau voucher)
            RefundCode = "REF-" + now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
            SubTotal = subTotal,
            ShippingFee = shippingFee,
            TotalAmount = orderTotalAmount,
            CustomerShippingPaid = customerShippingPaid,
            StatusId = statusId,
            IsDeleted = false,
            CreatedAt = now,
            ReturnToCustomerFeePaid = isUnpaid, // Đơn COD không yêu cầu khách trả phí gửi lại
            AdminNote = statusId switch
            {
                (byte)RefundStatusEnum.RefundDamage =>
                    $"Orders are damaged/lost during shipping (GHN updates Damage/Lost). No quality inspection is required.{ghnReason}{ghnCode}",
                (byte)RefundStatusEnum.RefundReceived =>
                    isUnpaid
                        ? $"System ReturnOnly: COD delivery failure return. Merchandise inspect upon shop receipt.{ghnReason}{ghnCode}"
                        : $"System return: customer did not receive the order. Merchandise inspect upon shop receipt.{ghnReason}{ghnCode}",
                _ => !string.IsNullOrWhiteSpace(refundOrder.CancelReason) ? $"GHN Failure Reason: {refundOrder.CancelReason}{ghnCode}" : null
            }
        };

        refund.RefundStatusHistories.Add(new RefundStatusHistory
        {
            StatusId = statusId,
            ChangedBy = null,
            Note = internalNote,
            CreatedAt = now
        });

        foreach (var detail in refundDetails)
        {
            refund.RefundDetails.Add(detail);
        }

        await _unitOfWork.Refunds.AddAsync(refund, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Kiểm tra phân công nhân viên xử lý đơn hàng nếu chưa có
        var activeAssignments = await _unitOfWork.OrderAssignments.GetActiveAssignmentsAsync(refund.OrderId, cancellationToken);
        var hasStaff = activeAssignments.Any(a => a.RoleId == 3);
        var hasMerch = activeAssignments.Any(a => a.RoleId == 4);

        if (!hasStaff || !hasMerch)
        {
            await _shiftAssignmentService.AutoAssignOrderAsync(refund.OrderId, cancellationToken);
        }

        return refund;
    }

    /// <summary>
    /// Khách hàng xem danh sách các yêu cầu hoàn tiền của mình, hỗ trợ phân trang và lọc theo trạng thái, ngày tháng.
    /// </summary>
    /// <param name="customerId">Mã ID tài khoản khách hàng.</param>
    /// <param name="filter">Bộ lọc yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang DTO yêu cầu hoàn tiền với trạng thái thân thiện.</returns>
    public async Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(int customerId, RefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        string? statusFilter = filter.RefundStatus;
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            var normalized = statusFilter.Trim().ToLowerInvariant();
            if (normalized == "processing")
            {
                statusFilter = $"{RefundStatuses.Approved},{RefundStatuses.PickupCreated},{RefundStatuses.Shipping},{RefundStatuses.Received},{RefundStatuses.InspectionPending},{RefundStatuses.ReturnShipmentCreated},{RefundStatuses.ReturningToCustomer}";
            }
            else if (normalized == "rejected")
            {
                statusFilter = $"{RefundStatuses.Rejected},{RefundStatuses.ReturnedToCustomer}";
            }
            else if (normalized == "cancelled")
            {
                statusFilter = $"{RefundStatuses.Cancelled},{RefundStatuses.ReturnToCustomerFailed}";
            }
            else
            {
                statusFilter = NormalizeRefundStatusFilter(statusFilter);
            }
        }

        filter.RefundStatus = statusFilter;
        var paginatedResult = await _unitOfWork.Refunds.GetRefundsAsync(customerId, filter, cancellationToken);

        if (paginatedResult.Items != null)
        {
            foreach (var item in paginatedResult.Items)
            {
                item.RefundStatus = MapToCustomerFacingStatus(item.RefundStatus);
            }
        }

        return paginatedResult;
    }

    /// <summary>
    /// Khách hàng xem thông tin chi tiết một yêu cầu hoàn tiền theo ID.
    /// </summary>
    /// <param name="customerId">Mã ID tài khoản khách hàng.</param>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO chi tiết yêu cầu hoàn tiền kèm lịch sử trạng thái thân thiện.</returns>
    public async Task<Result<RefundDto>> GetRefundByIdAsync(int customerId, int refundId, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null || refund.CustomerId != customerId)
            return Result<RefundDto>.NotFound("Refund", refundId);

        var dto = _mapper.Map<RefundDto>(refund);
        dto.RefundStatus = MapToCustomerFacingStatus(dto.RefundStatus);

        if (dto.StatusHistory != null)
        {
            var mappedHistory = new List<RefundStatusHistoryDto>();
            foreach (var h in dto.StatusHistory)
            {
                h.StatusName = MapToCustomerFacingStatus(h.StatusName);
                if (mappedHistory.Count == 0 || mappedHistory.Last().StatusName != h.StatusName)
                {
                    mappedHistory.Add(h);
                }
            }
            dto.StatusHistory = mappedHistory;
        }

        return Result<RefundDto>.Success(dto);
    }

    /// <summary>
    /// Khách hàng tự hủy yêu cầu hoàn tiền khi yêu cầu còn đang ở trạng thái chờ tiếp nhận (RefundRequested).
    /// </summary>
    /// <param name="customerId">Mã ID tài khoản khách hàng.</param>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO yêu cầu hoàn tiền sau khi đã cập nhật trạng thái Cancelled.</returns>
    public async Task<Result<RefundDto>> CancelRefundAsync(int customerId, int refundId, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null || refund.CustomerId != customerId)
            return Result<RefundDto>.NotFound("Refund", refundId);

        // Chỉ cho phép hủy khi yêu cầu đang ở trạng thái Requested
        if (refund.StatusId != (byte)RefundStatusEnum.RefundRequested)
            return Result<RefundDto>.BusinessError("Only 'Requested' refunds can be cancelled.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            refund.StatusId = (byte)RefundStatusEnum.RefundCancelled;
            refund.CancelledAt = DateTime.UtcNow;
            refund.UpdatedAt = DateTime.UtcNow;

            refund.RefundStatusHistories.Add(new RefundStatusHistory
            {
                StatusId = (byte)RefundStatusEnum.RefundCancelled,
                ChangedBy = customerId,
                Note = "Refund request cancelled by customer.",
                CreatedAt = DateTime.UtcNow
            });

            // Giải phóng khối lượng phân công cho nhân viên
            await _shiftAssignmentService.ReleaseCapacityAsync(refund.OrderId, cancellationToken);

            _unitOfWork.Refunds.Update(refund);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var dtoResult = _mapper.Map<RefundDto>(refund);
            dtoResult.RefundStatus = MapToCustomerFacingStatus(dtoResult.RefundStatus);
            return Result<RefundDto>.Success(dtoResult);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Quản trị viên / Nhân viên xem danh sách các yêu cầu hoàn tiền với bộ lọc nâng cao.
    /// </summary>
    /// <param name="filter">Bộ lọc danh sách dành cho admin/staff.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang DTO hoàn tiền đầy đủ trạng thái chi tiết.</returns>
    public async Task<PaginatedResponse<RefundListDto>> GetAdminRefundsAsync(AdminRefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        filter.RefundStatus = NormalizeRefundStatusFilter(filter.RefundStatus);
        return await _unitOfWork.Refunds.GetAdminRefundsAsync(filter, cancellationToken);
    }

    /// <summary>
    /// Quản trị viên / Nhân viên xem chi tiết một yêu cầu hoàn tiền, kèm theo kiểm tra phân quyền phân công nhân viên trực ca.
    /// </summary>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="currentUserId">Mã ID tài khoản nhân viên đang đăng nhập.</param>
    /// <param name="currentUserRoleId">Mã vai trò (Role ID) của nhân viên.</param>
    /// <param name="isAdmin">Cờ đánh dấu tài khoản là Quản trị viên (Admin) toàn quyền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO chi tiết yêu cầu hoàn tiền kèm thông tin phân công nhân viên.</returns>
    public async Task<Result<RefundDto>> AdminGetRefundByIdAsync(int refundId, int currentUserId, byte currentUserRoleId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null)
            return Result<RefundDto>.NotFound("Refund", refundId);

        // Kiểm tra phân quyền truy cập: Admin có quyền xem tất cả; Staff/Merchandise chỉ xem đơn được phân công
        if (!isAdmin)
        {
            var hasAssignment = await _unitOfWork.OrderAssignments.HasAssignmentForAccountAsync(
                refund.OrderId,
                currentUserId,
                currentUserRoleId,
                cancellationToken);

            if (!hasAssignment)
            {
                var isDirectAssignee = (currentUserRoleId == 3 && refund.Order?.AssignedToStaffId == currentUserId)
                                    || (currentUserRoleId == 4 && refund.Order?.AssignedToMerchId == currentUserId);

                if (!isDirectAssignee)
                {
                    return Result<RefundDto>.Failure("FORBIDDEN", "You are not authorized to view this refund request.");
                }
            }
        }

        var dto = _mapper.Map<RefundDto>(refund);

        var assignments = await _unitOfWork.OrderAssignments.GetActiveAssignmentsAsync(refund.OrderId, cancellationToken);
        var staffAssig = assignments.FirstOrDefault(a => a.RoleId == 3);
        var merchAssig = assignments.FirstOrDefault(a => a.RoleId == 4);

        dto.AssignedToStaffName = staffAssig?.Account?.AccountName
            ?? refund.Order.AssignedToStaff?.AccountName;
        dto.AssignedToMerchName = merchAssig?.Account?.AccountName
            ?? refund.Order.AssignedToMerch?.AccountName;

        return Result<RefundDto>.Success(dto);
    }

    /// <summary>
    /// Nhân viên / Quản trị viên cập nhật trạng thái yêu cầu hoàn tiền trong suốt quy trình xử lý (State Machine):
    /// - Kiểm tra ca trực OnDuty của nhân viên và quyền phân công xử lý đơn hàng.
    /// - Kiểm tra điều kiện chuyển trạng thái hợp lệ (RefundStatusTransitionValidator).
    /// - Phân quyền theo vai trò: Staff (duyệt/từ chối/hoàn tất), Merchandise (xác nhận nhận hàng kho/gửi kết quả kiểm tra chất lượng).
    /// - Luồng Phê duyệt (RefundApproved): Tính toán bên chịu phí ship, gọi GHN tính phí ước tính và xác định FinalRefundAmount.
    /// - Luồng Nhận hàng & Kiểm kho (RefundReceived -> RefundInspectionPending): Lưu phân rã số lượng đạt/hỏng (RestorableQuantity, FailedCustomerQty, FailedCarrierQty) và tính cấn trừ tiền hoàn.
    /// - Luồng Tạo vận đơn lấy hàng/giao trả (RefundPickupCreated / RefundReturnShipmentCreated): Tự động gọi API GHN tạo đơn lấy hàng hoặc đơn trả lại hàng hỏng cho khách.
    /// - Luồng Hoàn tất (RefundCompleted): Kích hoạt ExecuteCompletedSideEffects để cộng tiền vào ví và nhập lại tồn kho.
    /// </summary>
    /// <param name="staffId">Mã ID nhân viên thực hiện thao tác.</param>
    /// <param name="roleId">Mã vai trò của nhân viên (3: Staff, 4: Merchandise).</param>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="dto">Dữ liệu cập nhật trạng thái, ghi chú, kết quả kiểm tra chất lượng hàng hóa.</param>
    /// <param name="isAdmin">Cờ toàn quyền quản trị viên.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO yêu cầu hoàn tiền sau khi cập nhật trạng thái.</returns>
    public async Task<Result<RefundDto>> UpdateRefundStatusAsync(int staffId, byte roleId, int refundId, UpdateRefundStatusDto dto, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra tính hợp lệ của DTO qua FluentValidation
        var validation = await _updateRefundStatusValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<RefundDto>();

        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null)
            return Result<RefundDto>.NotFound("Refund", refundId);

        // 2. Kiểm tra ca trực và quyền phân công đơn hàng của nhân viên (nếu không phải Admin)
        if (!isAdmin)
        {
            // 2.1. Kiểm tra lịch làm việc và ca trực hiện tại
            var schedules = await _unitOfWork.WorkSchedules.GetByAccountAndDateAsync(staffId, _timeProvider.TodayVn, cancellationToken);
            var nowTime = _timeProvider.VnNow.TimeOfDay;
            var hasActiveShift = schedules.Any(s =>
                s.Status == "OnDuty" &&
                s.ShiftTemplate.StartTime <= nowTime &&
                s.ShiftTemplate.EndTime >= nowTime);

            if (!hasActiveShift)
            {
                return Result<RefundDto>.BusinessError("You do not have an active shift right now. Please contact the Admin to assign your shift before processing refunds.");
            }

            // 2.2. Kiểm tra phân công xử lý đơn hàng
            var hasAssignment = await _unitOfWork.OrderAssignments.HasActiveAssignmentAsync(
                refund.OrderId,
                staffId,
                roleId,
                cancellationToken);

            if (!hasAssignment)
            {
                hasAssignment = await _unitOfWork.OrderAssignments.IsLatestAssigneeWithNoActiveSuccessorAsync(
                    refund.OrderId,
                    staffId,
                    roleId,
                    cancellationToken);
            }

            if (!hasAssignment)
            {
                return Result<RefundDto>.BusinessError("You do not have an active assignment for this refund request.");
            }
        }

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(refund.OrderId, cancellationToken);
        if (order == null)
            return Result<RefundDto>.NotFound("Order", refund.OrderId);

        // 3. Kiểm tra trạng thái cuối cùng (Final State: Completed hoặc Cancelled không được thay đổi tiếp)
        if (refund.StatusId == (byte)RefundStatusEnum.RefundCompleted ||
            refund.StatusId == (byte)RefundStatusEnum.RefundCancelled)
        {
            var currentStatusName = refund.Status?.StatusName ?? ((RefundStatusEnum)refund.StatusId).ToString();
            return Result<RefundDto>.BusinessError($"Cannot change status from final state: {currentStatusName}.");
        }

        var newStatusId = MapStatusStringToId(dto.Status);
        if (newStatusId == null)
            return Result<RefundDto>.BusinessError($"Invalid status name '{dto.Status}'.");

        // Không được phép Reject khi đã tạo mã vận đơn GHN (chiều thu hồi hoặc chiều giao lại)
        if (newStatusId.Value == (byte)RefundStatusEnum.RefundRejected)
        {
            if (!string.IsNullOrWhiteSpace(refund.ShippingOrderCode) || !string.IsNullOrWhiteSpace(refund.ReturnShippingOrderCode))
            {
                return Result<RefundDto>.BusinessError(
                    "Cannot reject refund request after GHN shipping order has been created.");
            }
        }

        // Khi Thủ kho gửi kết quả kiểm tra chất lượng
        if (newStatusId.Value == (byte)RefundStatusEnum.RefundInspectionPending
            && (refund.StatusId == (byte)RefundStatusEnum.RefundReceived
                || refund.StatusId == (byte)RefundStatusEnum.RefundApproved
                || refund.StatusId == (byte)RefundStatusEnum.RefundRequested))
        {
            if (!dto.InspectionPassed.HasValue)
            {
                dto.InspectionPassed = true;
            }
            if (!string.IsNullOrWhiteSpace(dto.AdminNote) && string.IsNullOrWhiteSpace(dto.InspectionNote))
            {
                dto.InspectionNote = dto.AdminNote;
                dto.AdminNote = null;
            }

            // Thủ kho đề xuất trách nhiệm hư hỏng DamageResponsibility (Customer hoặc Carrier)
            if (!string.IsNullOrWhiteSpace(dto.DamageResponsibility))
            {
                if (!string.Equals(dto.DamageResponsibility, RefundDamageResponsibility.Customer, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(dto.DamageResponsibility, RefundDamageResponsibility.Carrier, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<RefundDto>.BusinessError(
                        "DamageResponsibility must be 'Customer' or 'Carrier'.");
                }
                refund.DamageResponsibility = dto.DamageResponsibility; // Proposal từ Merchandise
            }
        }

        var isSystemReturnRefund = IsSystemReturnRefund(refund);

        // 4. Kiểm tra tính hợp lệ của luồng chuyển trạng thái (State Transition Machine)
        var transitionError = RefundStatusTransitionValidator.GetTransitionError(
            refund.StatusId, newStatusId.Value, refund.RefundType, isSystemReturnRefund, isAdmin);
        if (transitionError is not null)
            return Result<RefundDto>.BusinessError(transitionError);

        // 5. Phân quyền hành động theo Role ID (3: Staff, 4: Merchandise)
        if (!isAdmin)
        {
            if (newStatusId.Value == (byte)RefundStatusEnum.RefundCompleted || newStatusId.Value == (byte)RefundStatusEnum.RefundReturnShipmentCreated)
            {
                if (roleId != 3)
                {
                    return Result<RefundDto>.BusinessError("Only Staff members are allowed to complete or manage return shipments for refund requests.");
                }
            }
            else if (newStatusId.Value == (byte)RefundStatusEnum.RefundApproved || newStatusId.Value == (byte)RefundStatusEnum.RefundRejected)
            {
                if (roleId != 3)
                {
                    return Result<RefundDto>.BusinessError("Only Staff members are allowed to approve or reject refund requests.");
                }
            }
            else if (newStatusId.Value == (byte)RefundStatusEnum.RefundReceived || newStatusId.Value == (byte)RefundStatusEnum.RefundInspectionPending)
            {
                if (roleId != 4)
                {
                    return Result<RefundDto>.BusinessError("Only Merchandise personnel are allowed to confirm receipt or submit inspection results for returned packages.");
                }
            }
        }

        // 6. Xử lý đặc thù cho luồng hoàn trả Hệ thống (System Return)
        if (isSystemReturnRefund)
        {
            var isPickupShippingStatus = newStatusId == (byte)RefundStatusEnum.RefundPickupCreated
                || newStatusId == (byte)RefundStatusEnum.RefundShipping;

            if (isPickupShippingStatus)
            {
                return Result<RefundDto>.BusinessError("System return refunds do not use pickup/shipping statuses.");
            }

            if (!string.IsNullOrWhiteSpace(dto.ShippingOrderCode))
            {
                return Result<RefundDto>.BusinessError("System return refunds do not accept a shipping order code.");
            }

            // Nhân viên hoàn tất đơn hoàn hệ thống: bắt buộc chọn có hoàn kèm phí ship không
            if (newStatusId == (byte)RefundStatusEnum.RefundCompleted
                && refund.StatusId == (byte)RefundStatusEnum.RefundInspectionPending)
            {
                if (!dto.IncludeShippingInRefund.HasValue)
                    return Result<RefundDto>.BusinessError(
                        "IncludeShippingInRefund is required when completing a system return. " +
                        "true = refund shipping fee, false = product amount only.");

                refund.IncludeShippingInRefund = dto.IncludeShippingInRefund.Value;

                var productRefundAmount = refund.RefundDetails.Sum(d => d.RefundAmount);
                refund.FinalRefundAmount = dto.IncludeShippingInRefund.Value
                    ? (refund.TotalAmount ?? refund.ApprovedAmount)
                    : productRefundAmount;
            }
        }

        // 7. Thủ kho / Nhân viên cập nhật kết quả kiểm tra chất lượng và số lượng sản phẩm phân rã
        if (newStatusId == (byte)RefundStatusEnum.RefundInspectionPending)
        {
            if (dto.RestockItems != null && dto.RestockItems.Count > 0)
            {
                foreach (var restockItem in dto.RestockItems)
                {
                    var detail = refund.RefundDetails.FirstOrDefault(d => d.ProductId == restockItem.ProductId);
                    if (detail == null)
                        return Result<RefundDto>.BusinessError($"Product ID {restockItem.ProductId} does not exist in this refund.");

                    if (restockItem.RestorableQuantity < 0 || restockItem.RestorableQuantity > detail.Quantity)
                        return Result<RefundDto>.BusinessError(
                            $"RestorableQuantity for Product ID {restockItem.ProductId} must be between 0 and {detail.Quantity}.");

                    if (restockItem.FailedCustomerQty < 0 || restockItem.FailedCustomerQty > detail.Quantity)
                        return Result<RefundDto>.BusinessError(
                            $"FailedCustomerQty for Product ID {restockItem.ProductId} must be between 0 and {detail.Quantity}.");

                    if (restockItem.FailedCarrierQty < 0 || restockItem.FailedCarrierQty > detail.Quantity)
                        return Result<RefundDto>.BusinessError(
                            $"FailedCarrierQty for Product ID {restockItem.ProductId} must be between 0 and {detail.Quantity}.");

                    // Tổng 3 nhóm số lượng phải bằng đúng số lượng hoàn trả ban đầu
                    if (restockItem.RestorableQuantity + restockItem.FailedCustomerQty + restockItem.FailedCarrierQty != detail.Quantity)
                    {
                        return Result<RefundDto>.BusinessError(
                            $"Sum of RestorableQuantity ({restockItem.RestorableQuantity}), FailedCustomerQty ({restockItem.FailedCustomerQty}), " +
                            $"and FailedCarrierQty ({restockItem.FailedCarrierQty}) must equal total product Quantity ({detail.Quantity}) for Product ID {restockItem.ProductId}.");
                    }

                    detail.RestorableQuantity = restockItem.RestorableQuantity;
                    detail.FailedCustomerQty = restockItem.FailedCustomerQty;
                    detail.FailedCarrierQty = restockItem.FailedCarrierQty;
                }
            }
            else
            {
                // Không gửi danh sách RestockItems -> mặc định kiểm tra Đạt (RestorableQuantity = Quantity)
                var isCarrierDamage = string.Equals(dto.DamageResponsibility, RefundDamageResponsibility.Carrier, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(refund.DamageResponsibility, RefundDamageResponsibility.Carrier, StringComparison.OrdinalIgnoreCase);

                foreach (var detail in refund.RefundDetails)
                {
                    if (isCarrierDamage)
                    {
                        detail.RestorableQuantity = 0;
                        detail.FailedCustomerQty = 0;
                        detail.FailedCarrierQty = detail.Quantity;
                    }
                    else
                    {
                        detail.RestorableQuantity = detail.Quantity;
                        detail.FailedCustomerQty = 0;
                        detail.FailedCarrierQty = 0;
                    }
                }
            }

            if (string.Equals(dto.DamageResponsibility, RefundDamageResponsibility.Carrier, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var detail in refund.RefundDetails)
                {
                    detail.RestorableQuantity = 0;
                    detail.FailedCustomerQty = 0;
                    detail.FailedCarrierQty = detail.Quantity;
                }
            }

            // Cập nhật cờ kiểm tra chất lượng chung ở Header
            refund.InspectionPassed = refund.RefundDetails.All(d => d.FailedCustomerQty == 0 && d.FailedCarrierQty == 0);

            var totalFailedCustomer = refund.RefundDetails.Sum(d => d.FailedCustomerQty);
            var totalFailedCarrier = refund.RefundDetails.Sum(d => d.FailedCarrierQty);

            if (totalFailedCustomer > 0)
            {
                refund.DamageResponsibility = RefundDamageResponsibility.Customer;
            }
            else if (totalFailedCarrier > 0)
            {
                refund.DamageResponsibility = RefundDamageResponsibility.Carrier;
            }
            else
            {
                refund.DamageResponsibility = null;
            }

            if (refund.RefundType == RefundTypes.ReturnOnly)
            {
                refund.ItemApprovedSubTotal = 0m;
                refund.ItemRejectedSubTotal = 0m;
                refund.ReturnToCustomerFee = 0m;
                refund.FinalRefundAmount = 0m;
                refund.ReturnToCustomerFeePaid = true;
                refund.CustomerResponseDeadline = null;
            }
            else
            {
                // 7.1. Tính toán giá trị sản phẩm được duyệt hoàn tiền và bị từ chối
                refund.ItemApprovedSubTotal = refund.RefundDetails.Sum(d => 
                    d.RefundAmount * (decimal)((d.RestorableQuantity ?? d.Quantity) + d.FailedCarrierQty) / d.Quantity);
                refund.ItemRejectedSubTotal = refund.RefundDetails.Sum(d => 
                    d.RefundAmount * (decimal)d.FailedCustomerQty / d.Quantity);

                // 7.2. Hoàn phí ship gốc: chỉ hoàn nếu Store chịu phí và không có bất kỳ sản phẩm nào bị khách làm hỏng
                var isCustomerBearingFee = string.Equals(refund.ReturnShippingFeeBy, RefundResponsibleParty.Customer, StringComparison.OrdinalIgnoreCase);
                var originalShipFeeRefund = (totalFailedCustomer > 0 || isCustomerBearingFee) ? 0m : refund.ShippingFee;

                // 7.3. Tổng số tiền hoàn dự kiến trước cấn trừ phí gửi trả lại (nếu có trừ phí ship hoàn trả)
                var totalRefundCandidate = refund.ItemApprovedSubTotal + originalShipFeeRefund;
                var effectiveCandidate = isCustomerBearingFee
                    ? Math.Max(0m, totalRefundCandidate - refund.ReturnShippingFee)
                    : totalRefundCandidate;

                // 7.4. Nếu có hàng hỏng do khách: gọi GHN ước tính phí gửi trả lại nhà cho khách
                if (totalFailedCustomer > 0)
                {
                    var shippingItems = refund.RefundDetails
                        .Where(d => d.FailedCustomerQty > 0)
                        .Select(d => new ShippingItem(
                            d.ProductId,
                            d.Product?.ProductName ?? "Return item",
                            d.Product?.Category?.CategoryName ?? "Toys",
                            (int)d.FailedCustomerQty,
                            d.UnitPrice,
                            d.Product?.ProductDetail?.WeightGram ?? _ghnOptions.DefaultItemWeight,
                            d.Product?.ProductDetail?.LengthCm ?? _ghnOptions.DefaultLength,
                            d.Product?.ProductDetail?.WidthCm ?? _ghnOptions.DefaultWidth,
                            d.Product?.ProductDetail?.HeightCm ?? _ghnOptions.DefaultHeight
                        )).ToList();

                    var package = GhnPackageCalculator.Calculate(
                        shippingItems,
                        _ghnOptions.DefaultItemWeight,
                        _ghnOptions.DefaultLength,
                        _ghnOptions.DefaultWidth,
                        _ghnOptions.DefaultHeight);

                    var feeRequest = new FeeRequestDTO
                    {
                        FromDistrictId = _shopAddress.DistrictId,
                        FromWardCode = _shopAddress.WardCode,
                        ToDistrictId = order.ShippingDistrictId,
                        ToWardCode = order.ShippingWardCode,
                        Weight = Math.Max(package.Weight, 1),
                        Length = package.Length,
                        Width = package.Width,
                        Height = package.Height,
                        ServiceTypeId = package.ServiceTypeId,
                        InsuranceValue = 0m,
                        CodValue = 0m,
                        Items = package.Items
                    };
                    var feeResult = await _ghnClient.GetFeeAsync(feeRequest, cancellationToken);
                    if (feeResult.IsSuccess && feeResult.Data != null)
                    {
                        refund.ReturnToCustomerFee = feeResult.Data.Fee;
                    }
                    else
                    {
                        refund.ReturnToCustomerFee = 30_000m;
                        _logger.LogWarning(
                            "GHN fee API failed for refund {RefundId} (district={DistrictId}, ward={WardCode}). " +
                            "Using fallback ReturnToCustomerFee={Fallback}. Error: {Error}",
                            refund.RefundId, order.ShippingDistrictId, order.ShippingWardCode,
                            30_000m, feeResult.ErrorMessage);
                    }

                    // 7.5. Thực hiện cơ chế cấn trừ phí gửi lại vào số tiền hoàn
                    if (effectiveCandidate >= refund.ReturnToCustomerFee)
                    {
                        // Tiền hoàn đủ bù phí gửi -> cấn trừ tự động và đánh dấu đã trả phí
                        refund.FinalRefundAmount = effectiveCandidate - refund.ReturnToCustomerFee;
                        refund.ReturnToCustomerFeePaid = true;
                        refund.CustomerResponseDeadline = null;
                    }
                    else
                    {
                        // Tiền hoàn không đủ bù phí -> yêu cầu khách thanh toán thêm phần chênh lệch trong 48h
                        refund.FinalRefundAmount = 0m;
                        refund.ReturnToCustomerFeePaid = false;
                        refund.CustomerResponseDeadline = DateTime.UtcNow.AddHours(48);
                    }
                }
                else
                {
                    refund.ReturnToCustomerFee = 0m;
                    refund.FinalRefundAmount = effectiveCandidate;
                    refund.ReturnToCustomerFeePaid = true;
                    refund.CustomerResponseDeadline = null;
                }
            }
        }

        // 8. Tự động gọi API GHN tạo đơn lấy hàng hoàn (Pickup) khi chuyển sang RefundPickupCreated
        if (newStatusId == (byte)RefundStatusEnum.RefundPickupCreated && string.IsNullOrWhiteSpace(dto.ShippingOrderCode))
        {
            var clientOrderCode = $"R-{refund.RefundCode ?? refund.RefundId.ToString()}";

            var paymentType = string.Equals(refund.ReturnShippingFeeBy, RefundResponsibleParty.Customer, StringComparison.OrdinalIgnoreCase)
                ? 1   // 1: Người gửi trả phí (Khách hàng trả tiền mặt lúc shipper lấy hàng)
                : 2;  // 2: Người nhận trả phí (Shop thanh toán / công nợ shop)

            var shippingItems = refund.RefundDetails.Select(x => new ShippingItem(
                x.ProductId,
                x.Product?.ProductName ?? "Return item",
                x.Product?.Category?.CategoryName ?? "Toys",
                x.Quantity,
                x.UnitPrice,
                x.Product?.ProductDetail?.WeightGram ?? _ghnOptions.DefaultItemWeight,
                x.Product?.ProductDetail?.LengthCm ?? _ghnOptions.DefaultLength,
                x.Product?.ProductDetail?.WidthCm ?? _ghnOptions.DefaultWidth,
                x.Product?.ProductDetail?.HeightCm ?? _ghnOptions.DefaultHeight
            )).ToList();

            var package = GhnPackageCalculator.Calculate(
                shippingItems,
                _ghnOptions.DefaultItemWeight,
                _ghnOptions.DefaultLength,
                _ghnOptions.DefaultWidth,
                _ghnOptions.DefaultHeight);

            var ghnRequest = new ShippingOrderCreateRequestDto
            {
                ClientOrderCode = clientOrderCode,
                PaymentTypeId = paymentType,
                ToName = _shopAddress.Name,
                ToPhone = _shopAddress.Phone,
                ToAddress = _shopAddress.AddressLine,
                ToDistrictId = _shopAddress.DistrictId,
                ToWardCode = _shopAddress.WardCode,
                FromName = refund.Customer?.AccountName ?? order.ShippingName,
                FromPhone = refund.Customer?.PhoneNumber ?? order.ShippingPhone,
                FromAddress = order.ShippingAddress,
                FromWardName = order.ShippingWardName,
                FromDistrictName = order.ShippingDistrictName,
                ServiceTypeId = package.ServiceTypeId,
                InsuranceValue = 0m,
                CodAmount = 0m,
                Weight = Math.Max(package.Weight, 1),
                Length = package.Length,
                Width = package.Width,
                Height = package.Height,
                Note = "Khach hang tra hang - Shop chiu phi",
                RequiredNote = "KHONGCHOXEMHANG",
                Items = package.Items.Select(i => new ShippingOrderCreateItemDto
                {
                    Name = i.Name,
                    Code = i.Code,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    Weight = i.Weight,
                    Length = i.Length,
                    Width = i.Width,
                    Height = i.Height
                }).ToList()
            };

            var ghnResult = await _ghnClient.CreateOrderAsync(ghnRequest, cancellationToken);
            if (!ghnResult.IsSuccess)
            {
                return Result<RefundDto>.Failure("GHN_CREATE_FAILED", $"Failed to create GHN pickup order: {ghnResult.ErrorMessage}");
            }

            dto.ShippingOrderCode = ghnResult.Data!.OrderCode;

            // Cập nhật phí thực tế từ GHN (ghi đè giá trị ước tính lúc Approve)
            if (ghnResult.Data.TotalFee > 0
                && string.Equals(refund.ReturnShippingFeeBy, RefundResponsibleParty.Customer, StringComparison.OrdinalIgnoreCase))
            {
                refund.ReturnShippingFee = ghnResult.Data.TotalFee;
                refund.FinalRefundAmount = ComputeFinalRefundAmount(
                    refund.ApprovedAmount, refund.ReturnShippingFee, refund.ReturnShippingFeeBy);
            }
        }

        // 9. Tự động gọi API GHN tạo đơn gửi trả lại sản phẩm từ chối cho khách khi chuyển sang RefundReturnShipmentCreated
        if (newStatusId == (byte)RefundStatusEnum.RefundReturnShipmentCreated && string.IsNullOrWhiteSpace(dto.ReturnShippingOrderCode))
        {
            var clientOrderCode = $"R2-{refund.RefundCode ?? refund.RefundId.ToString()}";

            var returnItems = refund.RefundDetails
                .Where(x => x.FailedCustomerQty > 0)
                .ToList();

            if (!returnItems.Any())
            {
                returnItems = refund.RefundDetails.ToList();
            }

            var shippingItems = returnItems.Select(x => new ShippingItem(
                x.ProductId,
                x.Product?.ProductName ?? "Return item",
                x.Product?.Category?.CategoryName ?? "Toys",
                (int)(x.FailedCustomerQty > 0 ? x.FailedCustomerQty : x.Quantity),
                x.UnitPrice,
                x.Product?.ProductDetail?.WeightGram ?? _ghnOptions.DefaultItemWeight,
                x.Product?.ProductDetail?.LengthCm ?? _ghnOptions.DefaultLength,
                x.Product?.ProductDetail?.WidthCm ?? _ghnOptions.DefaultWidth,
                x.Product?.ProductDetail?.HeightCm ?? _ghnOptions.DefaultHeight
            )).ToList();

            var package = GhnPackageCalculator.Calculate(
                shippingItems,
                _ghnOptions.DefaultItemWeight,
                _ghnOptions.DefaultLength,
                _ghnOptions.DefaultWidth,
                _ghnOptions.DefaultHeight);

            var ghnRequest = new ShippingOrderCreateRequestDto
            {
                ClientOrderCode = clientOrderCode,
                FromName = _shopAddress.Name,
                FromPhone = _shopAddress.Phone,
                FromAddress = _shopAddress.AddressLine,
                ToName = order.ShippingName,
                ToPhone = order.ShippingPhone,
                ToAddress = order.ShippingAddress,
                ToDistrictId = order.ShippingDistrictId,
                ToWardCode = order.ShippingWardCode,
                ServiceTypeId = package.ServiceTypeId,
                InsuranceValue = 0m,
                CodAmount = 0m,
                Weight = Math.Max(package.Weight, 1),
                Length = package.Length,
                Width = package.Width,
                Height = package.Height,
                Note = "Giao tra san pham tu choi refund - Shop chiu phi",
                RequiredNote = "KHONGCHOXEMHANG",
                Items = package.Items.Select(i => new ShippingOrderCreateItemDto
                {
                    Name = i.Name,
                    Code = i.Code,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    Weight = i.Weight,
                    Length = i.Length,
                    Width = i.Width,
                    Height = i.Height
                }).ToList()
            };

            var ghnResult = await _ghnClient.CreateOrderAsync(ghnRequest, cancellationToken);
            if (!ghnResult.IsSuccess)
            {
                return Result<RefundDto>.Failure("GHN_CREATE_FAILED", $"Failed to create GHN return shipment: {ghnResult.ErrorMessage}");
            }

            dto.ReturnShippingOrderCode = ghnResult.Data!.OrderCode;
        }

        // Kiểm tra chống trùng mã vận đơn với các yêu cầu hoàn tiền khác
        if (!string.IsNullOrWhiteSpace(dto.ShippingOrderCode))
        {
            var existingRefund = await _unitOfWork.Refunds.GetByShippingOrReturnOrderCodeAsync(dto.ShippingOrderCode, cancellationToken);
            if (existingRefund != null && existingRefund.RefundId != refund.RefundId)
            {
                return Result<RefundDto>.BusinessError($"Shipping Order Code '{dto.ShippingOrderCode}' is already assigned to another refund request.");
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.ReturnShippingOrderCode))
        {
            var existingRefund = await _unitOfWork.Refunds.GetByShippingOrReturnOrderCodeAsync(dto.ReturnShippingOrderCode, cancellationToken);
            if (existingRefund != null && existingRefund.RefundId != refund.RefundId)
            {
                return Result<RefundDto>.BusinessError($"Return Shipping Order Code '{dto.ReturnShippingOrderCode}' is already assigned to another refund request.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            refund.StatusId = newStatusId.Value;
            refund.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.ReturnDeliveryImageUrl))
            {
                refund.ReturnDeliveryImageUrl = dto.ReturnDeliveryImageUrl;
            }

            if (!string.IsNullOrWhiteSpace(dto.ReturnToCustomerImageUrl))
            {
                refund.ReturnToCustomerImageUrl = dto.ReturnToCustomerImageUrl;
            }

            if (!string.IsNullOrWhiteSpace(dto.ShippingOrderCode))
            {
                refund.ShippingOrderCode = dto.ShippingOrderCode;

                var existingTx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(dto.ShippingOrderCode, cancellationToken);
                if (existingTx is null)
                {
                    var tx = new ShippingProviderTransaction
                    {
                        OrderId = refund.OrderId,
                        RefundId = refund.RefundId,
                        Provider = "GHN",
                        ProviderOrderCode = dto.ShippingOrderCode,
                        Status = ShippingStatuses.ReadyToPick,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Orders.AddShippingTransactionAsync(tx, cancellationToken);
                }
                else if (existingTx.RefundId == null)
                {
                    // Bug Fix: existingTx là transaction gốc của đơn hàng chính (RefundId == null).
                    // Không được gán RefundId vào tx gốc vì sẽ làm GetOriginalOrderShippingTransaction()
                    // bỏ qua nó và dùng status refund để hiển thị trạng thái đơn hàng checkout → sai!
                    // Tạo tx mới riêng cho refund thay vì modify tx gốc.
                    _logger.LogWarning(
                        "ShippingOrderCode '{Code}' khớp tx gốc của đơn hàng (RefundId=null). Tạo tx mới cho RefundId={RefundId} để bảo vệ tx gốc.",
                        dto.ShippingOrderCode, refund.RefundId);
                    var newTx = new ShippingProviderTransaction
                    {
                        OrderId = refund.OrderId,
                        RefundId = refund.RefundId,
                        Provider = "GHN",
                        ProviderOrderCode = dto.ShippingOrderCode + $"_REF{refund.RefundId}",
                        Status = ShippingStatuses.ReadyToPick,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Orders.AddShippingTransactionAsync(newTx, cancellationToken);
                }
                else
                {
                    // tx này đã có RefundId != null (là tx của refund khác hoặc chính refund này)
                    existingTx.RefundId = refund.RefundId;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.ReturnShippingOrderCode))
            {
                refund.ReturnShippingOrderCode = dto.ReturnShippingOrderCode;

                var existingTx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(dto.ReturnShippingOrderCode, cancellationToken);
                if (existingTx is null)
                {
                    var tx = new ShippingProviderTransaction
                    {
                        OrderId = refund.OrderId,
                        RefundId = refund.RefundId,
                        Provider = "GHN",
                        ProviderOrderCode = dto.ReturnShippingOrderCode,
                        Status = ShippingStatuses.ReadyToPick,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Orders.AddShippingTransactionAsync(tx, cancellationToken);
                }
                else if (existingTx.RefundId == null)
                {
                    // Bug Fix: existingTx là transaction gốc của đơn hàng chính (RefundId == null).
                    // Không được gán RefundId vào tx gốc, tạo tx mới riêng cho refund.
                    _logger.LogWarning(
                        "ReturnShippingOrderCode '{Code}' khớp tx gốc của đơn hàng (RefundId=null). Tạo tx mới cho RefundId={RefundId} để bảo vệ tx gốc.",
                        dto.ReturnShippingOrderCode, refund.RefundId);
                    var newTx = new ShippingProviderTransaction
                    {
                        OrderId = refund.OrderId,
                        RefundId = refund.RefundId,
                        Provider = "GHN",
                        ProviderOrderCode = dto.ReturnShippingOrderCode + $"_RET{refund.RefundId}",
                        Status = ShippingStatuses.ReadyToPick,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Orders.AddShippingTransactionAsync(newTx, cancellationToken);
                }
                else
                {
                    // tx này đã có RefundId != null (là tx của refund khác hoặc chính refund này)
                    existingTx.RefundId = refund.RefundId;
                }
            }

            var isQualityCheckTransition = newStatusId == (byte)RefundStatusEnum.RefundInspectionPending
                                           || newStatusId == (byte)RefundStatusEnum.RefundReturnShipmentCreated;

            if (isQualityCheckTransition)
            {
                if (dto.InspectionPassed.HasValue)
                {
                    refund.InspectionPassed = dto.InspectionPassed.Value;
                }

                if (!string.IsNullOrWhiteSpace(dto.InspectionNote))
                {
                    refund.InspectionNote = dto.InspectionNote;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.AdminNote))
            {
                refund.AdminNote = dto.AdminNote;
            }

            // 10. Xử lý logic trạng thái Phê duyệt (RefundApproved)
            if (newStatusId == (byte)RefundStatusEnum.RefundApproved)
            {
                refund.ApprovedBy = staffId;
                refund.ApprovedAt = DateTime.UtcNow;

                if (refund.RefundType == RefundTypes.ReturnAndRefund)
                {
                    var suggestion = refund.RefundReason?.ResponsibleParty ?? RefundResponsibleParty.Store;
                    var finalFeeBy = !string.IsNullOrWhiteSpace(dto.ReturnShippingFeeBy)
                        ? dto.ReturnShippingFeeBy
                        : suggestion;

                    if (!string.Equals(finalFeeBy, suggestion, StringComparison.OrdinalIgnoreCase)
                        && string.IsNullOrWhiteSpace(dto.ReturnShippingFeeNote))
                    {
                        return Result<RefundDto>.BusinessError(
                            "ReturnShippingFeeNote is required when overriding the suggested responsible party.");
                    }

                    refund.ReturnShippingFeeBy = finalFeeBy;
                    refund.ReturnShippingFeeNote = dto.ReturnShippingFeeNote;

                    var itemSubTotal = refund.RefundDetails.Sum(d => d.RefundAmount);
                    bool isCustomerBearing = string.Equals(finalFeeBy, RefundResponsibleParty.Customer, StringComparison.OrdinalIgnoreCase);

                    if (isCustomerBearing)
                    {
                        // 1. Khách chịu phí (Customer fault/bears cost):
                        // - Không hoàn phí ship ban đầu
                        refund.ShippingFee = 0m;
                        refund.ApprovedAmount = itemSubTotal;
                        refund.TotalAmount = itemSubTotal;

                        // - Tính phí vận chuyển hoàn trả hàng về kho (GHN)
                        var shippingItems = refund.RefundDetails.Select(x => new ShippingItem(
                            x.ProductId,
                            x.Product?.ProductName ?? "Return item",
                            x.Product?.Category?.CategoryName ?? "Toys",
                            x.Quantity,
                            x.UnitPrice,
                            x.Product?.ProductDetail?.WeightGram ?? _ghnOptions.DefaultItemWeight,
                            x.Product?.ProductDetail?.LengthCm ?? _ghnOptions.DefaultLength,
                            x.Product?.ProductDetail?.WidthCm ?? _ghnOptions.DefaultWidth,
                            x.Product?.ProductDetail?.HeightCm ?? _ghnOptions.DefaultHeight
                        )).ToList();

                        var package = GhnPackageCalculator.Calculate(
                            shippingItems,
                            _ghnOptions.DefaultItemWeight,
                            _ghnOptions.DefaultLength,
                            _ghnOptions.DefaultWidth,
                            _ghnOptions.DefaultHeight);

                        var feeRequest = new FeeRequestDTO
                        {
                            FromDistrictId = order.ShippingDistrictId,
                            FromWardCode = order.ShippingWardCode,
                            ToDistrictId = _shopAddress.DistrictId,
                            ToWardCode = _shopAddress.WardCode,
                            ServiceTypeId = package.ServiceTypeId,
                            Weight = Math.Max(package.Weight, 1),
                            Length = package.Length,
                            Width = package.Width,
                            Height = package.Height,
                            InsuranceValue = 0m,
                            CodValue = 0m,
                            Items = package.Items
                        };

                        var feeResult = await _ghnClient.GetFeeAsync(feeRequest, cancellationToken);
                        refund.ReturnShippingFee = feeResult.IsSuccess && feeResult.Data != null
                            ? feeResult.Data.Fee
                            : 30_000m;

                        refund.FinalRefundAmount = ComputeFinalRefundAmount(
                            refund.ApprovedAmount, refund.ReturnShippingFee, refund.ReturnShippingFeeBy);

                        if (refund.FinalRefundAmount == 0m)
                        {
                            var warningNote = "[WARNING] Return shipping fee equals or exceeds approved amount. Customer will receive 0 refund.";
                            refund.AdminNote = string.IsNullOrEmpty(refund.AdminNote)
                                ? warningNote
                                : refund.AdminNote + " | " + warningNote;
                        }
                    }
                    else
                    {
                        // 2. Shop chịu phí (Store fault/bears cost):
                        // - Hoàn phí ship ban đầu nếu là trả toàn bộ đơn hàng
                        bool isFullReturn = order.OrderDetails.All(od => 
                            refund.RefundDetails.Any(rd => rd.ProductId == od.ProductId && rd.Quantity >= od.Quantity));
                        refund.ShippingFee = isFullReturn ? (order.ActualShippingFee ?? order.EstimatedShippingFee) : 0m;
                        refund.ReturnShippingFee = 0m;
                        refund.ApprovedAmount = itemSubTotal + refund.ShippingFee;
                        refund.TotalAmount = refund.ApprovedAmount;
                        refund.FinalRefundAmount = refund.ApprovedAmount;
                    }
                }
                else
                {
                    refund.ReturnShippingFee = 0m;
                    refund.ReturnShippingFeeBy = RefundResponsibleParty.Store;
                    refund.FinalRefundAmount = refund.ApprovedAmount;
                }
            }
            // 11. Xử lý logic trạng thái Từ chối (RefundRejected)
            else if (newStatusId == (byte)RefundStatusEnum.RefundRejected)
            {
                if (string.IsNullOrWhiteSpace(dto.RejectReason))
                {
                    return Result<RefundDto>.BusinessError("Reject reason is required when rejecting a refund.");
                }
                refund.RejectedAt = DateTime.UtcNow;
                refund.AdminNote = string.IsNullOrEmpty(refund.AdminNote)
                    ? $"Reject Reason: {dto.RejectReason}"
                    : $"{refund.AdminNote} | Reject Reason: {dto.RejectReason}";

                var history = new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    StatusId = order.StatusId,
                    ChangedBy = staffId,
                    Note = $"Refund Rejected: {dto.RejectReason}",
                    CreatedAt = DateTime.UtcNow
                };
                order.OrderStatusHistories.Add(history);
            }
            // 12. Xử lý logic trạng thái Hoàn tất (RefundCompleted)
            else if (newStatusId == (byte)RefundStatusEnum.RefundCompleted)
            {
                if (isSystemReturnRefund)
                {
                    if (!string.IsNullOrWhiteSpace(dto.DamageResponsibility))
                    {
                        refund.DamageResponsibility = dto.DamageResponsibility;
                    }

                    if (string.Equals(refund.DamageResponsibility, RefundDamageResponsibility.Carrier, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var detail in refund.RefundDetails)
                        {
                            detail.RestorableQuantity = 0;
                            detail.FailedCarrierQty = detail.Quantity;
                            detail.FailedCustomerQty = 0;
                        }

                        var carrierNote = "Goods damaged by GHN in transit. Full refund approved. No stock restoration. File carrier claim with GHN.";
                        refund.AdminNote = string.IsNullOrEmpty(refund.AdminNote)
                            ? carrierNote
                            : refund.AdminNote + " | " + carrierNote;
                    }
                }
                else if (refund.RefundType == RefundTypes.ReturnAndRefund)
                {
                    var customerFaultItems = refund.RefundDetails.Where(d => d.FailedCustomerQty > 0).ToList();

                    if (customerFaultItems.Any() && !refund.ReturnToCustomerFeePaid)
                    {
                        var originalShipFeeRefund = refund.RefundDetails.Any(d => d.FailedCustomerQty > 0) ? 0m : refund.ShippingFee;
                        var candidate = refund.ItemApprovedSubTotal + originalShipFeeRefund;
                        var shortfall = refund.ReturnToCustomerFee - candidate;
                        return Result<RefundDto>.BusinessError(
                            $"Cannot complete the refund. The customer has not paid the return shipping fee shortfall of {shortfall:N0} VND.");
                    }

                    if (customerFaultItems.Any() && !string.Equals(refund.CustomerResponse, "Disposed", StringComparison.OrdinalIgnoreCase))
                    {
                        newStatusId = (byte)RefundStatusEnum.RefundReturnShipmentCreated;
                        refund.StatusId = newStatusId.Value;

                        if (string.IsNullOrWhiteSpace(refund.ReturnShippingOrderCode))
                        {
                            var ghnItems = customerFaultItems.Select(x => new ShippingOrderCreateItemDto
                            {
                                Name = x.Product?.ProductName ?? "Return item",
                                Quantity = x.FailedCustomerQty,
                                Price = x.UnitPrice,
                                Weight = 500,
                                Code = x.ProductId.ToString()
                            }).ToList();

                            var clientOrderCode = $"R2-{refund.RefundCode ?? refund.RefundId.ToString()}";

                            var ghnRequest = new ShippingOrderCreateRequestDto
                            {
                                ClientOrderCode = clientOrderCode,
                                FromName = _shopAddress.Name,
                                FromPhone = _shopAddress.Phone,
                                FromAddress = _shopAddress.AddressLine,
                                ToName = order.ShippingName,
                                ToPhone = order.ShippingPhone,
                                ToAddress = order.ShippingAddress,
                                ToDistrictId = order.ShippingDistrictId,
                                ToWardCode = order.ShippingWardCode,
                                ServiceTypeId = 2, // Standard
                                InsuranceValue = 0m,
                                CodAmount = 0m,
                                Weight = 1000,
                                Length = 20,
                                Width = 15,
                                Height = 15,
                                Note = "Giao tra san pham tu choi refund (khach lam hong) - Khách đã thanh toán trước",
                                RequiredNote = "KHONGCHOXEMHANG",
                                Items = ghnItems
                            };

                            var ghnResult = await _ghnClient.CreateOrderAsync(ghnRequest, cancellationToken);
                            if (!ghnResult.IsSuccess)
                            {
                                return Result<RefundDto>.Failure("GHN_CREATE_FAILED", $"Failed to create GHN return shipment: {ghnResult.ErrorMessage}");
                            }

                            refund.ReturnShippingOrderCode = ghnResult.Data!.OrderCode;

                            var existingTx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(refund.ReturnShippingOrderCode, cancellationToken);
                            if (existingTx is null)
                            {
                                var tx = new ShippingProviderTransaction
                                {
                                    OrderId = refund.OrderId,
                                    Provider = "GHN",
                                    ProviderOrderCode = refund.ReturnShippingOrderCode,
                                    Status = ShippingStatuses.ReadyToPick,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                await _unitOfWork.Orders.AddShippingTransactionAsync(tx, cancellationToken);
                            }
                        }

                        var itemDetails = string.Join(", ", customerFaultItems.Select(d => $"{d.Product?.ProductName ?? d.ProductId.ToString()} (x{d.FailedCustomerQty})"));
                        var note = $"Partial refund: [{itemDetails}] returned to customer. Refunded: {refund.FinalRefundAmount:N0} VND. Return Shipping Fee: {refund.ReturnToCustomerFee:N0} VND (Paid).";
                        refund.AdminNote = string.IsNullOrEmpty(refund.AdminNote) ? note : refund.AdminNote + " | " + note;
                        refund.DamageResponsibility = RefundDamageResponsibility.Customer;
                    }
                    else if (customerFaultItems.Any() && string.Equals(refund.CustomerResponse, "Disposed", StringComparison.OrdinalIgnoreCase))
                    {
                        var itemDetails = string.Join(", ", customerFaultItems.Select(d => $"{d.Product?.ProductName ?? d.ProductId.ToString()} (x{d.FailedCustomerQty})"));
                        var note = $"Partial refund: [{itemDetails}] customer fault items are DISPOSED (customer chose to dispose or failed to pay fee). Refunded: {refund.FinalRefundAmount:N0} VND.";
                        refund.AdminNote = string.IsNullOrEmpty(refund.AdminNote) ? note : refund.AdminNote + " | " + note;
                        refund.DamageResponsibility = RefundDamageResponsibility.Customer;
                    }
                    else if (refund.InspectionPassed == false)
                    {
                        refund.DamageResponsibility = RefundDamageResponsibility.Carrier;
                        var carrierNote = "Goods damaged by carrier in return transit. Refund approved. No stock restoration. File carrier claim with GHN.";
                        refund.AdminNote = string.IsNullOrEmpty(refund.AdminNote)
                            ? carrierNote
                            : refund.AdminNote + " | " + carrierNote;
                    }
                }

                if (refund.StatusId == (byte)RefundStatusEnum.RefundInspectionPending)
                {
                    refund.InspectionPassed = refund.InspectionPassed ?? true;
                    if (string.IsNullOrWhiteSpace(refund.InspectionNote) && !string.IsNullOrWhiteSpace(dto.AdminNote))
                    {
                        refund.InspectionNote = dto.AdminNote;
                    }
                }

                if (refund.FinalRefundAmount == 0m && refund.ApprovedAmount > 0m && refund.ItemApprovedSubTotal == 0m && refund.ItemRejectedSubTotal == 0m)
                {
                    refund.FinalRefundAmount = string.Equals(refund.ReturnShippingFeeBy, RefundResponsibleParty.Customer, StringComparison.OrdinalIgnoreCase)
                        ? Math.Max(0m, refund.ApprovedAmount - refund.ReturnShippingFee)
                        : refund.ApprovedAmount;
                }

                refund.CompletedAt = DateTime.UtcNow;
                // Kích hoạt các side effect: cộng ví và cập nhật lại tồn kho sản phẩm
                await ExecuteCompletedSideEffects(refund, order, cancellationToken);
            }

            // Ghi nhận lịch sử trạng thái mới
            string historyNote;
            if (!string.IsNullOrEmpty(dto.RejectReason))
            {
                historyNote = $"Reject Reason: {dto.RejectReason}";
            }
            else if (isSystemReturnRefund && newStatusId == (byte)RefundStatusEnum.RefundCompleted && dto.IncludeShippingInRefund.HasValue)
            {
                historyNote = dto.IncludeShippingInRefund.Value
                    ? $"System Return Complete — Shipping fee included. FinalRefundAmount={refund.FinalRefundAmount:N0}."
                    : $"System Return Complete — Shipping fee excluded. FinalRefundAmount={refund.FinalRefundAmount:N0}.";
            }
            else if (!string.IsNullOrEmpty(dto.AdminNote))
            {
                historyNote = $"Note: {dto.AdminNote}";
            }
            else if (!string.IsNullOrEmpty(dto.InspectionNote))
            {
                historyNote = $"Quality Inspection Note: {dto.InspectionNote}";
            }
            else
            {
                historyNote = $"Status changed to {dto.Status} by staff.";
            }

            refund.RefundStatusHistories.Add(new RefundStatusHistory
            {
                StatusId = newStatusId.Value,
                ChangedBy = staffId,
                Note = historyNote,
                CreatedAt = DateTime.UtcNow
            });

            var shouldReleaseCapacity = newStatusId == (byte)RefundStatusEnum.RefundCompleted ||
                                         newStatusId == (byte)RefundStatusEnum.RefundRejected ||
                                         newStatusId == (byte)RefundStatusEnum.RefundReturnedToCustomer ||
                                         newStatusId == (byte)RefundStatusEnum.RefundReturnToCustomerFailed ||
                                         newStatusId == (byte)RefundStatusEnum.RefundReturnShipmentCreated;

            if (shouldReleaseCapacity)
            {
                await _shiftAssignmentService.ReleaseCapacityAsync(refund.OrderId, cancellationToken);
            }

            _unitOfWork.Refunds.Update(refund);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Bắn thông báo realtime cho khách hàng
            var eventType = dto.Status switch
            {
                RefundStatuses.Approved => NotificationEventTypes.RefundApproved,
                RefundStatuses.Rejected => NotificationEventTypes.RefundRejected,
                RefundStatuses.Completed => NotificationEventTypes.RefundCompleted,
                _ => null
            };

            if (eventType is not null)
            {
                await _eventPublisher.PublishAsync("Refund", refundId.ToString(),
                    eventType,
                    new
                    {
                        refundId,
                        orderId = order.OrderId,
                        orderCode = order.OrderCode,
                        customerId = refund.CustomerId,
                        status = dto.Status,
                        amount = refund.ApprovedAmount,
                    },
                    CancellationToken.None);
            }

            // Luồng hoàn trả hệ thống: Báo cho nhân viên CSKH hoàn tất hoàn tiền ví khi thủ kho đã kiểm hàng xong
            if (isSystemReturnRefund && newStatusId == (byte)RefundStatusEnum.RefundInspectionPending)
            {
                await _eventPublisher.PublishAsync("Refund", refundId.ToString(),
                    NotificationEventTypes.StaffSystemRefundReady,
                    new
                    {
                        refundId,
                        orderId  = order.OrderId,
                        orderCode = order.OrderCode,
                    },
                    CancellationToken.None);
            }

            var updatedRefund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
            return Result<RefundDto>.Success(_mapper.Map<RefundDto>(updatedRefund));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Thực hiện các tác vụ phụ trợ (Side Effects) khi yêu cầu hoàn tiền hoàn tất thành công (RefundCompleted):
    /// 1. Tự động cộng tiền FinalRefundAmount vào Ví khách hàng (Wallet) chống trùng lặp Idempotency.
    /// 2. Cập nhật trạng thái thanh toán đơn hàng (PaymentStatus = Refunded hoặc PartiallyRefunded).
    /// 3. Cập nhật trạng thái đơn hàng Orders.StatusId sang Refunded (nếu hoàn tiền toàn bộ).
    /// 4. Nhập kho hoàn trả (Restock) lại số lượng sản phẩm nguyên vẹn (RestorableQuantity) vào kho chính và kho Flash Sale.
    /// </summary>
    /// <param name="refund">Thực thể OrderRefund.</param>
    /// <param name="order">Thực thể đơn hàng Order.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    private async Task ExecuteCompletedSideEffects(OrderRefund refund, Order order, CancellationToken cancellationToken)
    {
        // 1. Cộng tiền hoàn vào ví điện tử của khách hàng
        var orderRefundKey = WalletRefundKeys.ForOrder(order.OrderCode);
        var alreadyCredited =
            await _unitOfWork.Orders.HasCompletedRefundWalletCreditForOrderAsync(order.OrderId, cancellationToken)
            || await _unitOfWork.Orders.ExistsWalletTransactionByIdempotencyKeyAsync(orderRefundKey, cancellationToken);

        if (!alreadyCredited && refund.FinalRefundAmount > 0m)
        {
            await _walletRefundCreditor.CreditRefundAsync(
                refund.CustomerId,
                refund.FinalRefundAmount,
                order.OrderCode,
                order.OrderId,
                cancellationToken,
                orderRefundKey);
        }

        var txnId = await _unitOfWork.Orders.GetWalletTransactionIdByIdempotencyKeyAsync(orderRefundKey, cancellationToken);
        if (txnId.HasValue)
            refund.WalletTransactionId = (int)txnId.Value;

        // 2. Xác định xem đơn hàng được hoàn toàn bộ hay hoàn một phần
        bool isFullRefund = true;
        foreach (var originalDetail in order.OrderDetails)
        {
            var refundedItem = refund.RefundDetails.FirstOrDefault(rd => rd.ProductId == originalDetail.ProductId);
            if (refundedItem == null 
                || refundedItem.FailedCustomerQty > 0
                || refundedItem.Quantity < originalDetail.Quantity)
            {
                isFullRefund = false;
                break;
            }
        }

        if (refund.FinalRefundAmount > 0m)
        {
            if (isFullRefund)
            {
                // 3. Đơn hoàn toàn bộ: PaymentStatus = REFUNDED, OrderStatus = Refunded
                order.PaymentStatus = PaymentStatuses.Refunded;

                var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);
                byte refundedStatusId = statusMap.GetValueOrDefault("Refunded", (byte)OrderStatus.Refunded);

                order.StatusId = refundedStatusId;
                order.UpdatedAt = DateTime.UtcNow;

                var history = new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    StatusId = refundedStatusId,
                    ChangedBy = refund.ApprovedBy ?? refund.RequestedBy, // Admin or System
                    Note = "Refund Completed",
                    CreatedAt = DateTime.UtcNow
                };
                order.OrderStatusHistories.Add(history);
            }
            else
            {
                // 4. Đơn hoàn một phần: PaymentStatus = PARTIALLY_REFUNDED
                order.PaymentStatus = PaymentStatuses.PartiallyRefunded;
                order.UpdatedAt = DateTime.UtcNow;

                var history = new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    StatusId = order.StatusId,
                    ChangedBy = refund.ApprovedBy ?? refund.RequestedBy, // Admin or System
                    Note = "Partial Refund Completed",
                    CreatedAt = DateTime.UtcNow
                };
                order.OrderStatusHistories.Add(history);
            }
        }

        // 5. Khôi phục số lượng tồn kho sản phẩm (nếu hàng hóa không bị mất/hư hỏng trong vận chuyển và chưa từng được hoàn kho lúc Cancel đơn)
        bool isAlreadyStockRestoredOnCancel = order.CancelledAt.HasValue
            || string.Equals(refund.RefundType, RefundTypes.RefundOnly, StringComparison.OrdinalIgnoreCase);

        bool isLostOrDamaged = string.Equals(order.CancelReason, OrderCancelReasons.LostInTransit, StringComparison.OrdinalIgnoreCase)
            || string.Equals(order.CancelReason, OrderCancelReasons.DamagedInTransit, StringComparison.OrdinalIgnoreCase)
            || refund.RefundStatusHistories.Any(h => h.StatusId == (byte)RefundStatusEnum.RefundDamage)
            || string.Equals(refund.DamageResponsibility, RefundDamageResponsibility.Carrier, StringComparison.OrdinalIgnoreCase);

        if (!isLostOrDamaged && !isAlreadyStockRestoredOnCancel)
        {
            foreach (var item in refund.RefundDetails)
            {
                // Dùng RestorableQuantity nếu có (được Thủ kho kiểm tra), fallback về Quantity cho các dòng cũ
                short restoreQty = item.RestorableQuantity ?? item.Quantity;

                if (restoreQty <= 0) continue;

                // Hoàn lại tồn kho chính
                await _unitOfWork.Orders.AdjustStockAsync(item.ProductId, restoreQty, cancellationToken);
                var originalDetail = order.OrderDetails.FirstOrDefault(od => od.ProductId == item.ProductId);
                // Nếu sản phẩm thuộc khung giờ Flash Sale thì hoàn lại tồn kho Flash Sale tương ứng
                if (originalDetail != null && originalDetail.SlotProductId.HasValue)
                {
                    await _unitOfWork.Orders.AdjustFlashSaleStockAsync(originalDetail.SlotProductId.Value, -restoreQty, 0, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Quản trị viên phân công lại nhân viên CSKH hoặc Thủ kho phụ trách xử lý yêu cầu hoàn tiền.
    /// </summary>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="dto">Thông tin tài khoản và vai trò nhân viên được phân công mới.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Kết quả thực hiện phân công lại.</returns>
    public async Task<Result> ReassignRefundAsync(int refundId, ToyStore.Application.DTOs.Assignments.ReassignOrderRequestDto dto, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null)
            return Result.NotFound("Refund", refundId);

        return await _shiftAssignmentService.ReassignOrderAsync(refund.OrderId, dto, cancellationToken);
    }

    /// <summary>
    /// Khách hàng thanh toán phí chênh lệch vận chuyển hoàn trả hàng hỏng về nhà (Shortfall Return Fee) bằng số dư Ví:
    /// - Chỉ áp dụng trong giai đoạn Kiểm tra chất lượng (RefundInspectionPending).
    /// - Kiểm tra số dư ví khách hàng đủ để trừ phí chênh lệch shortfall.
    /// - Thực hiện trừ ví và tạo giao dịch WalletTransaction (Debit).
    /// - Cập nhật ReturnToCustomerFeePaid = true và phản hồi khách hàng là AcceptReturn.
    /// </summary>
    /// <param name="customerId">Mã ID tài khoản khách hàng.</param>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO chi tiết yêu cầu hoàn tiền sau khi đã thanh toán phí thành công.</returns>
    public async Task<Result<RefundDto>> PayReturnFeeAsync(int customerId, int refundId, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null || refund.CustomerId != customerId)
            return Result<RefundDto>.NotFound("Refund", refundId);

        if (refund.StatusId != (byte)RefundStatusEnum.RefundInspectionPending)
            return Result<RefundDto>.BusinessError("Return shipping fee payment is only allowed during the Inspection Pending stage.");

        if (refund.ReturnToCustomerFeePaid)
            return Result<RefundDto>.BusinessError("Return shipping fee is already paid.");

        var customerFaultItems = refund.RefundDetails.Where(d => d.FailedCustomerQty > 0).ToList();
        if (!customerFaultItems.Any())
            return Result<RefundDto>.BusinessError("This refund does not contain any customer-fault items.");

        var originalShipFeeRefund = refund.RefundDetails.Any(d => d.FailedCustomerQty > 0) ? 0m : refund.ShippingFee;
        var candidate = refund.ItemApprovedSubTotal + originalShipFeeRefund;
        var shortfall = refund.ReturnToCustomerFee - candidate;

        if (shortfall <= 0)
        {
            refund.ReturnToCustomerFeePaid = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<RefundDto>.Success(_mapper.Map<RefundDto>(refund));
        }

        // 1. Kiểm tra ví của khách hàng
        var wallet = await _unitOfWork.Wallets.GetByAccountIdAsync(customerId, cancellationToken);
        if (wallet == null)
            return Result<RefundDto>.BusinessError("Wallet not found for this customer.");

        if (string.Equals(wallet.Status, "Frozen", StringComparison.OrdinalIgnoreCase))
            return Result<RefundDto>.BusinessError("Your wallet is currently frozen. Please contact support to unfreeze your wallet before making payments.");

        if (wallet.Balance < shortfall)
            return Result<RefundDto>.BusinessError($"Insufficient wallet balance. You need {shortfall:N0} VND but balance is {wallet.Balance:N0} VND. Please top up your wallet.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 2. Trừ số dư ví khách hàng
            var balanceBefore = wallet.Balance;
            wallet.Balance -= shortfall;
            wallet.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Wallets.UpdateWallet(wallet);

            // Ghi nhận giao dịch thanh toán phí gửi hàng vào bảng WalletTransaction
            var walletTx = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                AccountId = customerId,
                RelatedOrderId = refund.OrderId,
                TxnType = WalletTxnTypes.Payment,
                Direction = WalletTxnDirections.Debit,
                Amount = shortfall,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                Method = "Internal",
                Reason = $"Payment for return shipping shortfall on Refund #{refund.RefundCode}",
                IdempotencyKey = $"PayShortfall-{refund.RefundCode}-{DateTime.UtcNow.Ticks}",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            await _unitOfWork.Orders.AddWalletTransactionAsync(walletTx, cancellationToken);

            refund.ReturnToCustomerFeePaid = true;
            refund.CustomerResponse = "AcceptReturn";
            refund.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var updatedRefund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
            return Result<RefundDto>.Success(_mapper.Map<RefundDto>(updatedRefund));
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Kiểm tra xem yêu cầu hoàn trả có phải do hệ thống tự động tạo khi GHN giao hàng thất bại (Luồng B) hay không.
    /// </summary>
    /// <param name="refund">Thực thể OrderRefund.</param>
    /// <returns>true nếu là nguồn System và có hàng hoàn vật lý, ngược lại là false.</returns>
    private static bool IsSystemReturnRefund(OrderRefund refund)
        => string.Equals(refund.RefundSource, RefundSources.System, StringComparison.OrdinalIgnoreCase)
           && !string.Equals(refund.RefundType, RefundTypes.RefundOnly, StringComparison.OrdinalIgnoreCase);
}
