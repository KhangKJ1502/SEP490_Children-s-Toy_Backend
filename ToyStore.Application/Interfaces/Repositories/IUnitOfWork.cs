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

<<<<<<< HEAD
    IWishlistRepository Wishlists { get; }
=======
    IReviewRepository Reviews { get; }
>>>>>>> 8e31d08c7157223382caaa2f1dbe38480be08287

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
