namespace ToyStore.Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    ISuperCategoryRepository SuperCategories { get; }

    ICategoryRepository Categories { get; }

    IAccountRepository Accounts { get; }

    IRoleRepository Roles { get; }

    IVoucherRepository Vouchers { get; }

    IBrandRepository Brands { get; }

    IProductRepository Products { get; }

    ITemplateRepository Templates { get; }

    IPromotionRepository Promotions { get; }

    ICampaignRepository Campaigns { get; }
    IAddressRepository Addresses { get; }

    ICustomerChildRepository CustomerChildren { get; }

    IBlogRepository Blogs { get; }

    ICartRepository Carts { get; }

    IOrderRepository Orders { get; }

    IWishlistRepository Wishlists { get; }
    IReviewRepository Reviews { get; }
    IDeliveryRepository Deliveries { get; }
    IUserPreferenceRepository UserPreferences { get; }
    IProductFollowerRepository ProductFollowers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
