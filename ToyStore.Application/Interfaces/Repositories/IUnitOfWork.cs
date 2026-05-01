namespace ToyStore.Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    ISuperCategoryRepository SuperCategories { get; }

    ICategoryRepository Categories { get; }

    IAccountRepository Accounts { get; }

    IVoucherRepository Vouchers { get; }

    IBrandRepository Brands { get; }

    IProductRepository Products { get; }

    ITemplateRepository Templates { get; }

    IPromotionRepository Promotions { get; }
    
    ICampaignRepository Campaigns { get; }

    IBlogRepository Blogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
