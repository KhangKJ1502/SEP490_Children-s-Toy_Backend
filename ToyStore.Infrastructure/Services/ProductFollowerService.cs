using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class ProductFollowerService : IProductFollowerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;

    public ProductFollowerService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITimeProvider timeProvider)
    {
        _unitOfWork         = unitOfWork;
        _currentUserService = currentUserService;
        _timeProvider       = timeProvider;
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
}
