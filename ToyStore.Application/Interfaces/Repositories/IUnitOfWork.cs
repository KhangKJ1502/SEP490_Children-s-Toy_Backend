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
    IRefundRepository Refunds { get; }
    IRefundImageRepository RefundImages { get; }
    IWalletRepository Wallets { get; }
    IWalletTransactionRepository WalletTransactions { get; }
    IShiftTemplateRepository ShiftTemplates { get; }
    IWorkScheduleRepository WorkSchedules { get; }
    IStaffShiftCapacityRepository StaffShiftCapacities { get; }
    IOrderAssignmentRepository OrderAssignments { get; }
    IOrderQueueRepository OrderQueues { get; }
    ICampaignApprovalLogRepository CampaignApprovalLogs { get; }
    ICampaignScheduleRepository CampaignSchedules { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    void Detach<T>(T entity) where T : class;

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
