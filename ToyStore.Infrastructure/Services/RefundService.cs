using AutoMapper;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

public class RefundService : IRefundService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IGhnClient _ghnClient;
    private readonly GhnOptions _ghnOptions;
    private readonly ShopAddressOptions _shopAddress;

    public RefundService(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        IDomainEventPublisher eventPublisher,
        IGhnClient ghnClient,
        IOptions<GhnOptions> ghnOptions,
        IOptions<ShopAddressOptions> shopAddress)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
        _ghnClient = ghnClient;
        _ghnOptions = ghnOptions.Value;
        _shopAddress = shopAddress.Value;
    }

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
            _ => null
        };
    }

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
            _ => status
        };
    }

    public async Task<List<RefundReasonDto>> GetRefundReasonsAsync(CancellationToken cancellationToken = default)
    {
        var reasons = await _unitOfWork.Refunds.GetActiveReasonsAsync(cancellationToken);
        return _mapper.Map<List<RefundReasonDto>>(reasons);
    }

    public async Task<Result<RefundDto>> CreateRefundAsync(int customerId, CreateRefundDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(dto.OrderId, cancellationToken);
        if (order == null || order.AccountId != customerId)
            return Result<RefundDto>.NotFound("Order", dto.OrderId);

        // Validations
        if (order.StatusId != (byte)OrderStatus.Completed)
            return Result<RefundDto>.BusinessError("Order must be in Completed status to request a refund.");

        if (order.CompletedAt == null || (DateTime.UtcNow - order.CompletedAt.Value).TotalDays > 3)
            return Result<RefundDto>.BusinessError("Refund requests must be submitted within 3 days of order completion.");

        // Wallet check: customer must have an Active wallet to receive the refund amount
        var wallet = await _unitOfWork.Wallets.GetByAccountIdAsync(customerId, cancellationToken);
        if (wallet == null)
            return Result<RefundDto>.BusinessError("You must create a wallet before requesting a refund. Please set up your wallet at My Wallet.");
        if (!string.Equals(wallet.Status, "Active", StringComparison.OrdinalIgnoreCase))
            return Result<RefundDto>.BusinessError($"Your wallet is currently {wallet.Status}. Only an Active wallet can receive refunds. Please resolve your wallet status before requesting a refund.");

        var existingRefunds = await _unitOfWork.Refunds.GetAdminRefundsAsync(new AdminRefundFilterDto { OrderId = dto.OrderId, PageSize = 100 }, cancellationToken);

        if (existingRefunds.Items.Any())
        {
            return Result<RefundDto>.BusinessError("Only 1 refund request is allowed per order lifecycle.");
        }

        // Process return items (support partial returns)
        var returnItems = new List<CreateRefundItemDto>();
        if (dto.Items == null || !dto.Items.Any())
        {
            // Default to returning all items in the order (Full Refund)
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

        // Verify items exist and quantities do not exceed original purchased quantities
        var refundDetails = new List<RefundDetail>();
        var discountRatio = order.SubTotal > 0 ? (order.VoucherDiscountAmount / order.SubTotal) : 0m;

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

            // Proportionate voucher discount adjustment
            var itemRefundAmount = Math.Round(item.Quantity * originalDetail.UnitPrice * (1 - discountRatio), 0);

            refundDetails.Add(new RefundDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = originalDetail.UnitPrice,
                RefundAmount = itemRefundAmount,
                CreatedAt = DateTime.UtcNow
            });
        }

        var subTotal = refundDetails.Sum(d => d.RefundAmount);
        var totalAmount = subTotal; // Shipping fee is 0 by default

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var refund = new OrderRefund
            {
                OrderId = dto.OrderId,
                RefundReasonId = dto.RefundReasonId,
                ReasonDetails = dto.ReasonDetails,
                CustomerId = customerId,
                RequestedBy = customerId,
                ApprovedAmount = totalAmount, 
                RefundCode = "REF-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                SubTotal = subTotal,
                ShippingFee = 0,
                TotalAmount = totalAmount,
                StatusId = (byte)RefundStatusEnum.RefundRequested,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            // Set initial state log in children histories
            refund.RefundStatusHistories.Add(new RefundStatusHistory
            {
                StatusId = (byte)RefundStatusEnum.RefundRequested,
                ChangedBy = customerId,
                Note = "Refund request created by customer.",
                CreatedAt = DateTime.UtcNow
            });

            // Associate refund details
            foreach (var detail in refundDetails)
            {
                refund.RefundDetails.Add(detail);
            }

            await _unitOfWork.Refunds.AddAsync(refund, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken); // Save to get RefundId

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

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Notify staff about the new refund request
            await _eventPublisher.PublishAsync("Refund", refund.RefundId.ToString(),
                NotificationEventTypes.RefundNewRequest,
                new { refundId = refund.RefundId, orderId = dto.OrderId, orderCode = order.OrderCode, customerId },
                CancellationToken.None);

            var createdRefund = await _unitOfWork.Refunds.GetByIdAsync(refund.RefundId, cancellationToken);
            return Result<RefundDto>.Success(_mapper.Map<RefundDto>(createdRefund));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(int customerId, RefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        var adminFilter = new AdminRefundFilterDto
        {
            Page = filter.Page,
            PageSize = filter.PageSize,
            CustomerId = customerId,
            RefundStatus = NormalizeRefundStatusFilter(filter.RefundStatus),
            OrderId = filter.OrderId,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate
        };
        return await _unitOfWork.Refunds.GetAdminRefundsAsync(adminFilter, cancellationToken);
    }

    public async Task<Result<RefundDto>> GetRefundByIdAsync(int customerId, int refundId, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null || refund.CustomerId != customerId)
            return Result<RefundDto>.NotFound("Refund", refundId);

        return Result<RefundDto>.Success(_mapper.Map<RefundDto>(refund));
    }

    public async Task<Result<RefundDto>> CancelRefundAsync(int customerId, int refundId, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null || refund.CustomerId != customerId)
            return Result<RefundDto>.NotFound("Refund", refundId);

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

            _unitOfWork.Refunds.Update(refund);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result<RefundDto>.Success(_mapper.Map<RefundDto>(refund));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaginatedResponse<RefundListDto>> GetAdminRefundsAsync(AdminRefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        filter.RefundStatus = NormalizeRefundStatusFilter(filter.RefundStatus);
        return await _unitOfWork.Refunds.GetAdminRefundsAsync(filter, cancellationToken);
    }

    public async Task<Result<RefundDto>> AdminGetRefundByIdAsync(int refundId, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null)
            return Result<RefundDto>.NotFound("Refund", refundId);

        var dto = _mapper.Map<RefundDto>(refund);

        var assignments = await _unitOfWork.OrderAssignments.GetActiveAssignmentsAsync(refund.OrderId, cancellationToken);
        var staffAssig = assignments.FirstOrDefault(a => a.RoleId == 3);
        var merchAssig = assignments.FirstOrDefault(a => a.RoleId == 4);

        if (staffAssig != null)
        {
            dto.AssignedToStaffName = staffAssig.Account?.AccountName;
        }
        if (merchAssig != null)
        {
            dto.AssignedToMerchName = merchAssig.Account?.AccountName;
        }

        return Result<RefundDto>.Success(dto);
    }

    public async Task<Result<RefundDto>> UpdateRefundStatusAsync(int staffId, int refundId, UpdateRefundStatusDto dto, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null)
            return Result<RefundDto>.NotFound("Refund", refundId);

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(refund.OrderId, cancellationToken);
        if (order == null)
            return Result<RefundDto>.NotFound("Order", refund.OrderId);

        // Final state checking
        if (refund.StatusId == (byte)RefundStatusEnum.RefundCompleted || 
            refund.StatusId == (byte)RefundStatusEnum.RefundCancelled)
        {
            var currentStatusName = refund.Status?.StatusName ?? ((RefundStatusEnum)refund.StatusId).ToString();
            return Result<RefundDto>.BusinessError($"Cannot change status from final state: {currentStatusName}.");
        }

        var newStatusId = MapStatusStringToId(dto.Status);
        if (newStatusId == null)
            return Result<RefundDto>.BusinessError($"Invalid status name '{dto.Status}'.");

        // Automatically call GHN API to generate waybill if new status is RefundPickupCreated
        // and ShippingOrderCode is not manually entered!
        if (newStatusId == (byte)RefundStatusEnum.RefundPickupCreated && string.IsNullOrWhiteSpace(dto.ShippingOrderCode))
        {
            var clientOrderCode = $"R-{refund.RefundCode ?? refund.RefundId.ToString()}";

            var ghnRequest = new ShippingOrderCreateRequestDto
            {
                ClientOrderCode = clientOrderCode,
                ToName          = _shopAddress.Name,
                ToPhone         = _shopAddress.Phone,
                ToAddress       = _shopAddress.AddressLine,
                ToDistrictId    = _shopAddress.DistrictId,
                ToWardCode      = _shopAddress.WardCode,
                FromName        = refund.Customer?.AccountName ?? order.ShippingName,
                FromPhone       = refund.Customer?.PhoneNumber ?? order.ShippingPhone,
                FromAddress     = order.ShippingAddress,
                FromWardName    = order.ShippingWardName,
                FromDistrictName = order.ShippingDistrictName,
                ServiceTypeId   = 2, // Standard
                InsuranceValue  = 0m,
                CodAmount       = 0m,
                Weight          = 1000,
                Length          = 20,
                Width           = 15,
                Height          = 15,
                Note            = "Khach hang tra hang - Shop chiu phi",
                RequiredNote    = "KHONGCHOXEMHANG",
                Items           = refund.RefundDetails.Select(x => new ShippingOrderCreateItemDto
                {
                    Name     = x.Product?.ProductName ?? "Sản phẩm hoàn trả",
                    Quantity = x.Quantity,
                    Price    = x.UnitPrice,
                    Weight   = 500,
                    Code     = x.ProductId.ToString()
                }).ToList()
            };

            var ghnResult = await _ghnClient.CreateOrderAsync(ghnRequest, cancellationToken);
            if (!ghnResult.IsSuccess)
            {
                return Result<RefundDto>.Failure("GHN_CREATE_FAILED", $"Failed to create GHN pickup order: {ghnResult.ErrorMessage}");
            }

            dto.ShippingOrderCode = ghnResult.Data!.OrderCode;
        }

        if (!string.IsNullOrWhiteSpace(dto.ShippingOrderCode))
        {
            var existingRefund = await _unitOfWork.Refunds.GetByShippingOrderCodeAsync(dto.ShippingOrderCode, cancellationToken);
            if (existingRefund != null && existingRefund.RefundId != refund.RefundId)
            {
                return Result<RefundDto>.BusinessError($"Shipping Order Code '{dto.ShippingOrderCode}' is already assigned to another refund request.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            refund.StatusId = newStatusId.Value;
            refund.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.ShippingOrderCode))
            {
                refund.ShippingOrderCode = dto.ShippingOrderCode;
            }

            if (!string.IsNullOrWhiteSpace(dto.AdminNote))
            {
                refund.AdminNote = dto.AdminNote;
            }

            if (newStatusId == (byte)RefundStatusEnum.RefundApproved)
            {
                refund.ApprovedBy = staffId;
                refund.ApprovedAt = DateTime.UtcNow;
            }
            else if (newStatusId == (byte)RefundStatusEnum.RefundRejected)
            {
                refund.RejectedAt = DateTime.UtcNow;
                refund.ReasonDetails = string.IsNullOrEmpty(refund.ReasonDetails)
                    ? $"Reject Reason: {dto.RejectReason}"
                    : $"{refund.ReasonDetails} | Reject Reason: {dto.RejectReason}";
            }
            else if (newStatusId == (byte)RefundStatusEnum.RefundCompleted)
            {
                refund.CompletedAt = DateTime.UtcNow;
                await ExecuteCompletedSideEffects(refund, order, cancellationToken);
            }

            // Log status transitions in the history logs
            refund.RefundStatusHistories.Add(new RefundStatusHistory
            {
                StatusId = newStatusId.Value,
                ChangedBy = staffId,
                Note = string.IsNullOrEmpty(dto.RejectReason) 
                    ? (string.IsNullOrEmpty(dto.AdminNote) ? $"Status changed to {dto.Status} by staff." : $"Note: {dto.AdminNote}") 
                    : $"Reject Reason: {dto.RejectReason}",
                CreatedAt = DateTime.UtcNow
            });

            _unitOfWork.Refunds.Update(refund);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Notify customer about refund status change
            var eventType = dto.Status switch
            {
                RefundStatuses.Approved  => NotificationEventTypes.RefundApproved,
                RefundStatuses.Rejected  => NotificationEventTypes.RefundRejected,
                RefundStatuses.Completed => NotificationEventTypes.RefundCompleted,
                _                        => null
            };

            if (eventType is not null)
            {
                await _eventPublisher.PublishAsync("Refund", refundId.ToString(),
                    eventType,
                    new
                    {
                        refundId,
                        orderId      = order.OrderId,
                        orderCode    = order.OrderCode,
                        customerId   = refund.CustomerId,
                        status       = dto.Status,
                        amount       = refund.ApprovedAmount,
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

    private async Task ExecuteCompletedSideEffects(OrderRefund refund, Order order, CancellationToken cancellationToken)
    {
        // 1. Wallet: Balance += ApprovedAmount
        var wallet = await _unitOfWork.Wallets.GetByAccountIdAsync(refund.CustomerId, cancellationToken);
        if (wallet == null)
        {
            wallet = new Wallet
            {
                AccountId = refund.CustomerId,
                Currency = "VND",
                Balance = refund.ApprovedAmount,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                LastTransactionAt = DateTime.UtcNow
            };
            await _unitOfWork.Wallets.CreateAsync(wallet, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken); // to get WalletId
        }
        else
        {
            wallet.Balance += refund.ApprovedAmount;
            wallet.LastTransactionAt = DateTime.UtcNow;
            wallet.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Wallets.UpdateWallet(wallet);
        }

        // 2. WalletTransactions
        var txn = new WalletTransaction
        {
            WalletId = wallet.WalletId,
            AccountId = refund.CustomerId,
            RelatedOrderId = order.OrderId,
            TxnType = WalletTxnTypes.Refund,
            Direction = WalletTxnDirections.Credit,
            Amount = refund.ApprovedAmount,
            BalanceBefore = wallet.Balance - refund.ApprovedAmount,
            BalanceAfter = wallet.Balance,
            Method = "Internal",
            Status = "Completed",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        await _unitOfWork.WalletTransactions.AddAsync(txn, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        refund.WalletTransactionId = txn.WalletTransactionId;
        refund.StatusId = (byte)RefundStatusEnum.RefundCompleted;

        bool isFullRefund = true;
        foreach (var originalDetail in order.OrderDetails)
        {
            var refundedItem = refund.RefundDetails.FirstOrDefault(rd => rd.ProductId == originalDetail.ProductId);
            if (refundedItem == null || refundedItem.Quantity < originalDetail.Quantity)
            {
                isFullRefund = false;
                break;
            }
        }

        if (isFullRefund)
        {
            // 3. Orders.PaymentStatus = REFUNDED
            order.PaymentStatus = "REFUNDED";

            // 4. Orders.StatusID -> status Refunded
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
            // 3. Orders.PaymentStatus = PARTIALLY_REFUNDED
            order.PaymentStatus = "PARTIALLY_REFUNDED";
            order.UpdatedAt = DateTime.UtcNow;

            // We do not change order status (remains Completed) but log history entry for partial refund
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
        // order is tracked by EF, no need to call Update

        // 5. Restore Inventory strictly for the items returned
        foreach (var item in refund.RefundDetails)
        {
            await _unitOfWork.Products.AdjustStockAsync(item.ProductId, item.Quantity, cancellationToken);
            
            // Find if this product was sold as part of flash sale slot in original order
            var originalDetail = order.OrderDetails.FirstOrDefault(od => od.ProductId == item.ProductId);
            if (originalDetail != null && originalDetail.SlotProductId.HasValue)
            {
                // Refund means stock was deducted from SoldQuantity
                await _unitOfWork.Orders.AdjustFlashSaleStockAsync(originalDetail.SlotProductId.Value, -item.Quantity, 0, cancellationToken);
            }
        }
    }
}
