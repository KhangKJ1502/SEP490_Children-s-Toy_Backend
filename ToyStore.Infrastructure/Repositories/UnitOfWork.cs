using Microsoft.EntityFrameworkCore.Storage;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Unit of Work quản lý transaction và save changes.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SEP490ToyStoreContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(
        SEP490ToyStoreContext context,
        IVoucherRepository vouchers,
        ISuperCategoryRepository superCategories,
        ICategoryRepository categories,
        IAccountRepository accounts,
        IRoleRepository roles,
        IBrandRepository brands,
        IBlogRepository blogs,
        IPromotionRepository promotions,
        ICampaignRepository campaigns,
        IProductRepository products,
        ITemplateRepository templates,
        IAddressRepository addresses,
        ICartRepository carts,
        IOrderRepository orders,
        IWishlistRepository wishlists,
        IReviewRepository reviews,
        ICustomerChildRepository customerChildren,
        IDeliveryRepository deliveries,
        IUserPreferenceRepository userPreferences,
        IProductFollowerRepository productFollowers,
        IRefundRepository refunds,
        IRefundImageRepository refundImages,
        IWalletRepository wallets,
        IWalletTransactionRepository walletTransactions)
    {
        _context = context;
        Vouchers = vouchers;
        SuperCategories = superCategories;
        Categories = categories;
        Accounts = accounts;
        Roles = roles;
        Brands = brands;
        Blogs = blogs;
        Products = products;
        Templates = templates;
        Promotions = promotions;
        Campaigns = campaigns;
        Addresses = addresses;
        Carts = carts;
        Orders = orders;
        Wishlists = wishlists;
        Reviews = reviews;
        CustomerChildren = customerChildren;
        Deliveries = deliveries;
        UserPreferences = userPreferences;
        ProductFollowers = productFollowers;
        Refunds = refunds;
        RefundImages = refundImages;
        Wallets = wallets;
        WalletTransactions = walletTransactions;
    }

    public IVoucherRepository Vouchers { get; }

    public ISuperCategoryRepository SuperCategories { get; }

    public ICategoryRepository Categories { get; }

    public IAccountRepository Accounts { get; }

    public IRoleRepository Roles { get; }

    public IBrandRepository Brands { get; }

    public IBlogRepository Blogs { get; }

    public IProductRepository Products { get; }

    public ITemplateRepository Templates { get; }

    public IPromotionRepository Promotions { get; }

    public ICampaignRepository Campaigns { get; }

    public IAddressRepository Addresses { get; }

    public ICartRepository Carts { get; }

    public ICustomerChildRepository CustomerChildren { get; }

    public IOrderRepository Orders { get; }

    public IWishlistRepository Wishlists { get; }

    public IReviewRepository Reviews { get; }

    public IDeliveryRepository Deliveries { get; }

    public IUserPreferenceRepository UserPreferences { get; }

    public IProductFollowerRepository ProductFollowers { get; }

    public IRefundRepository Refunds { get; }

    public IRefundImageRepository RefundImages { get; }

    public IWalletRepository Wallets { get; }

    public IWalletTransactionRepository WalletTransactions { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}
