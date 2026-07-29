using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services;

public class ProductFollowerService : IProductFollowerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;
    private readonly SEP490ToyStoreContext _dbContext;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IUserPreferenceChecker _prefChecker;

    public ProductFollowerService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITimeProvider timeProvider,
        SEP490ToyStoreContext dbContext,
        INotificationDispatcher dispatcher,
        IUserPreferenceChecker prefChecker)
    {
        _unitOfWork         = unitOfWork;
        _currentUserService = currentUserService;
        _timeProvider       = timeProvider;
        _dbContext          = dbContext;
        _dispatcher         = dispatcher;
        _prefChecker        = prefChecker;
    }

    /// <summary>
    /// Đăng ký theo dõi sản phẩm.
    /// </summary>
    public async Task<Result> FollowProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0) return Result.Failure("UNAUTHORIZED", "Please login to follow the product.");

        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (product == null || product.IsDeleted) return Result.NotFound("Product", productId);

        var existing = await _unitOfWork.ProductFollowers.GetByAccountAndProductAsync(accountId, productId, cancellationToken);
        if (existing != null) return Result.Success(); // Đã theo dõi rồi

        var follower = new ProductFollower
        {
            AccountId = accountId,
            ProductId = productId,
            CreatedAt = _timeProvider.UtcNow
        };

        await _unitOfWork.ProductFollowers.AddAsync(follower, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Hủy theo dõi sản phẩm.
    /// </summary>
    public async Task<Result> UnfollowProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0) return Result.Failure("UNAUTHORIZED", "Please login to unfollow the product.");

        var existing = await _unitOfWork.ProductFollowers.GetByAccountAndProductAsync(accountId, productId, cancellationToken);
        if (existing == null) return Result.Success(); // Đã không theo dõi rồi

        _unitOfWork.ProductFollowers.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Kiểm tra trạng thái theo dõi.
    /// </summary>
    public async Task<Result<bool>> IsFollowingAsync(int productId, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0) return Result<bool>.Success(false);

        var existing = await _unitOfWork.ProductFollowers.GetByAccountAndProductAsync(accountId, productId, cancellationToken);
        return Result<bool>.Success(existing != null);
    }

    /// <summary>
    /// Lấy danh sách ID các sản phẩm đang theo dõi.
    /// </summary>
    public async Task<Result<List<int>>> GetFollowedProductIdsAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0) return Result<List<int>>.Unauthorized();

        var followers = await _unitOfWork.ProductFollowers.GetByAccountIdAsync(accountId, cancellationToken);
        var productIds = followers.Select(f => f.ProductId).ToList();

        return Result<List<int>>.Success(productIds);
    }

    /// <summary>
    /// Gửi thông báo báo hàng về cho tất cả người theo dõi sản phẩm (nếu sản phẩm đủ điều kiện in-stock).
    /// </summary>
    public async Task NotifyFollowersIfBackInStockAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (product == null || product.IsDeleted || product.Quantity <= 0 || product.ProductStatus != "Active")
        {
            return;
        }

        var pendingFollowers = await _dbContext.ProductFollowers
            .Where(f => f.ProductId == productId && f.NotifiedAt == null)
            .ToListAsync(cancellationToken);

        if (pendingFollowers.Count == 0) return;

        foreach (var follower in pendingFollowers)
        {
            if (!await _prefChecker.CanSendAsync(follower.AccountId, PreferenceKeys.StockAlerts, cancellationToken))
            {
                continue;
            }

            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = follower.AccountId,
                RecipientType      = RecipientTypes.Customer,
                NotificationType   = NotificationTypes.Stock,
                TemplateCode       = NotificationTemplates.ProductBackInStock,
                Placeholders       = new Dictionary<string, string>
                {
                    ["ProductName"] = product.ProductName,
                    ["Price"]       = $"{product.Price:N0}",
                },
                ReferenceId  = $"{follower.ProductId}:{follower.AccountId}",
                SendBell     = true,
                SendEmail    = true,
                ActionTarget = $"/products/{follower.ProductId}",
            }, cancellationToken);

            follower.NotifiedAt = _timeProvider.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Reset trạng thái NotifiedAt = null khi sản phẩm hết hàng để chuẩn bị cho lần báo hàng về tiếp theo.
    /// </summary>
    public async Task ResetFollowerNotificationStateAsync(int productId, CancellationToken cancellationToken = default)
    {
        var notifiedFollowers = await _dbContext.ProductFollowers
            .Where(f => f.ProductId == productId && f.NotifiedAt != null)
            .ToListAsync(cancellationToken);

        if (notifiedFollowers.Count == 0) return;

        foreach (var follower in notifiedFollowers)
        {
            follower.NotifiedAt = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
