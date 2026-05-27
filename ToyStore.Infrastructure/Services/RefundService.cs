using AutoMapper;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

public class RefundService : IRefundService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDomainEventPublisher _eventPublisher;

    public RefundService(IUnitOfWork unitOfWork, IMapper mapper, IDomainEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
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

        if (existingRefunds.Items.Count >= 2)
            return Result<RefundDto>.BusinessError("Maximum of 2 refund requests allowed per order.");

        if (existingRefunds.Items.Any(r => r.RefundStatus == RefundStatuses.Requested || r.RefundStatus == RefundStatuses.Approved))
            return Result<RefundDto>.BusinessError("An active refund request already exists for this order.");

        if (existingRefunds.Items.Any(r => r.RefundStatus == RefundStatuses.Rejected))
            return Result<RefundDto>.BusinessError("Previous refund request was rejected. Cannot create a new one.");

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
                ApprovedAmount = order.TotalAmount, // Based on confirmation
                RefundStatus = RefundStatuses.Requested,
                CreatedAt = DateTime.UtcNow
            };

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

    public async Task<OrderRefund?> CreateSystemRefundForDeliveryFailAsync(
        Order order, byte refundReasonId, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Refunds.GetAdminRefundsAsync(
            new AdminRefundFilterDto { OrderId = order.OrderId, PageSize = 10 },
            cancellationToken);

        if (existing.Items.Any(r =>
                r.RefundStatus is RefundStatuses.Requested or RefundStatuses.Approved or RefundStatuses.Completed))
        {
            return null;
        }

        var refund = new OrderRefund
        {
            OrderId = order.OrderId,
            RefundReasonId = refundReasonId,
            ReasonDetails = "Auto-created: GHN delivery failure return",
            CustomerId = order.AccountId,
            RequestedBy = null,
            ApprovedAmount = order.TotalAmount,
            RefundStatus = RefundStatuses.Requested,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Refunds.AddAsync(refund, cancellationToken);
        return refund;
    }

    public async Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(int customerId, RefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        var adminFilter = new AdminRefundFilterDto
        {
            Page = filter.Page,
            PageSize = filter.PageSize,
            CustomerId = customerId,
            RefundStatus = filter.RefundStatus,
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

        if (refund.RefundStatus != RefundStatuses.Requested)
            return Result<RefundDto>.BusinessError("Only 'Requested' refunds can be cancelled.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            refund.RefundStatus = RefundStatuses.Cancelled;
            refund.UpdatedAt = DateTime.UtcNow;
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
        return await _unitOfWork.Refunds.GetAdminRefundsAsync(filter, cancellationToken);
    }

    public async Task<Result<RefundDto>> AdminGetRefundByIdAsync(int refundId, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null)
            return Result<RefundDto>.NotFound("Refund", refundId);

        return Result<RefundDto>.Success(_mapper.Map<RefundDto>(refund));
    }

    public async Task<Result<RefundDto>> UpdateRefundStatusAsync(int staffId, int refundId, UpdateRefundStatusDto dto, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund == null)
            return Result<RefundDto>.NotFound("Refund", refundId);

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(refund.OrderId, cancellationToken);
        if (order == null)
            return Result<RefundDto>.NotFound("Order", refund.OrderId);

        // Status transition validations
        if (refund.RefundStatus == RefundStatuses.Requested)
        {
            if (dto.Status != RefundStatuses.Approved && dto.Status != RefundStatuses.Rejected)
                return Result<RefundDto>.BusinessError($"Valid transitions from Requested are Approved or Rejected.");
        }
        else if (refund.RefundStatus == RefundStatuses.Approved)
        {
            if (dto.Status != RefundStatuses.Completed && dto.Status != RefundStatuses.Rejected)
                return Result<RefundDto>.BusinessError($"Valid transitions from Approved are Completed or Rejected.");
        }
        else
        {
            return Result<RefundDto>.BusinessError($"Cannot change status from {refund.RefundStatus}.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            refund.RefundStatus = dto.Status;
            refund.UpdatedAt = DateTime.UtcNow;

            if (dto.Status == RefundStatuses.Approved)
            {
                refund.ApprovedBy = staffId;
            }
            else if (dto.Status == RefundStatuses.Rejected)
            {
                refund.ReasonDetails = string.IsNullOrEmpty(refund.ReasonDetails)
                    ? $"Reject Reason: {dto.RejectReason}"
                    : $"{refund.ReasonDetails} | Reject Reason: {dto.RejectReason}";
            }
            else if (dto.Status == RefundStatuses.Completed)
            {
                await ExecuteCompletedSideEffects(refund, order, cancellationToken);
            }

            _unitOfWork.Refunds.Update(refund);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Notify customer about refund status change
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
        refund.RefundStatus = RefundStatuses.Completed;

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
        // Ensure adding history is valid, if there is a repository for it.
        // For simplicity, we can update Order.OrderStatusHistories
        order.OrderStatusHistories.Add(history);
        // order is tracked by EF, no need to call Update

        // 5. Restore Inventory
        var orderDetails = order.OrderDetails;
        foreach (var item in orderDetails)
        {
            await _unitOfWork.Products.AdjustStockAsync(item.ProductId, item.Quantity, cancellationToken);

            if (item.SlotProductId.HasValue)
            {
                // Refund means order was Paid/Completed, so stock was deducted from SoldQuantity
                await _unitOfWork.Orders.AdjustFlashSaleStockAsync(item.SlotProductId.Value, -item.Quantity, 0, cancellationToken);
            }
        }
    }
}
