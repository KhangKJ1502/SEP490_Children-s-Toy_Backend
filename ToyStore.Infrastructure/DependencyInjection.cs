
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ToyStore.Application.DTOs.Accounts;
using ToyStore.Application.DTOs.Addresses;
using ToyStore.Application.DTOs.Auth;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.DTOs.Brands;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.DTOs.Carts;
using ToyStore.Application.DTOs.CustomerChildren;
using ToyStore.Application.DTOs.Customers;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Application.DTOs.Profiles;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Application.DTOs.Templates;
using ToyStore.Application.DTOs.Wallets;
using ToyStore.Application.DTOs.Shifts;
using ToyStore.Application.DTOs.Assignments;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Mappings;
using ToyStore.Application.Validators.Accounts;
using ToyStore.Application.Validators.Addresses;
using ToyStore.Application.Validators.Auth;
using ToyStore.Application.Validators.Blogs;
using ToyStore.Application.Validators.Brands;
using ToyStore.Application.Validators.Campaigns;
using ToyStore.Application.Validators.Carts;
using ToyStore.Application.Validators.CustomerChildren;
using ToyStore.Application.Validators.Customers;
using ToyStore.Application.Validators.Orders;
using ToyStore.Application.Validators.Profiles;
using ToyStore.Application.Validators.Reviews;
using ToyStore.Application.Validators.Templates;
using ToyStore.Application.Validators.Wallets;
using ToyStore.Application.Validators.Shifts;
using ToyStore.Application.Validators.Assignments;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ToyStore.Application;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;
using ToyStore.Infrastructure.Options;
using ToyStore.Infrastructure.Repositories;
using ToyStore.Infrastructure.Services;
using ToyStore.Infrastructure.Services.Resolvers;


namespace ToyStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SEP490ToyStoreContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        var redisConnectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false;
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));


        // Đăng ký FluentValidation
        services.AddValidatorsFromAssemblyContaining<Application.Validators.SuperCategories.CreateSuperCategoryValidator>();

        // Đăng ký AutoMapper — quét toàn bộ Profile trong Application (gồm ShiftSchedulingProfile)
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ShiftSchedulingProfile).Assembly));

        services.AddScoped<IValidator<CreateBrandDto>, CreateBrandValidator>();
        services.AddScoped<IValidator<UpdateBrandDto>, UpdateBrandValidator>();
        services.AddScoped<IValidator<CreateBlogDto>, CreateBlogValidator>();
        services.AddScoped<IValidator<UpdateBlogDto>, UpdateBlogValidator>();
        services.AddScoped<IValidator<SubmitBlogDto>, SubmitBlogValidator>();
        services.AddScoped<IValidator<ApproveBlogDto>, ApproveBlogValidator>();
        services.AddScoped<IValidator<UpdateBlogFeaturedDto>, UpdateBlogFeaturedValidator>();
        services.AddScoped<IValidator<UpdateBlogReviewPermissionDto>, UpdateBlogReviewPermissionValidator>();


        services.AddScoped<IValidator<CreateAccountDto>, CreateAccountValidator>();
        services.AddScoped<IValidator<UpdateAccountStatusDto>, UpdateAccountStatusValidator>();
        services.AddScoped<IValidator<UpdateAccountPasswordDto>, UpdateAccountPasswordValidator>();
        services.AddScoped<IValidator<UpdateCustomerDto>, UpdateCustomerValidator>();
        services.AddScoped<IValidator<CreateTemplateDto>, CreateTemplateValidator>();
        services.AddScoped<IValidator<UpdateTemplateDto>, UpdateTemplateValidator>();
        services.AddScoped<IValidator<CreateCampaignDto>, CreateCampaignValidator>();
        services.AddScoped<IValidator<UpdateProfileDto>, UpdateProfileValidator>();
        services.AddScoped<IValidator<ChangeCustomerPasswordDto>, ChangeCustomerPasswordValidator>();
        services.AddScoped<IValidator<UpdateCampaignDto>, UpdateCampaignValidator>();
        services.AddScoped<IValidator<ChangePasswordDto>, ChangePasswordValidator>();
        services.AddScoped<IValidator<UpdateCampaignDto>, UpdateCampaignValidator>();
        services.AddScoped<IValidator<ReviewCampaignDto>, ReviewCampaignValidator>();
        services.AddScoped<IValidator<ScheduleCampaignDto>, ScheduleCampaignValidator>();
        services.AddScoped<IValidator<RescheduleCampaignDto>, RescheduleCampaignValidator>();

        // Validator Google OAuth
        services.AddScoped<IValidator<GoogleLoginDto>, GoogleLoginValidator>();
        services.AddScoped<IValidator<GoogleRegisterDto>, GoogleRegisterValidator>();
        services.AddScoped<IValidator<CreateAddressDto>, CreateAddressValidator>();
        services.AddScoped<IValidator<UpdateAddressDto>, UpdateAddressValidator>();
        services.AddScoped<IValidator<CreateChildDto>, CreateChildValidator>();
        services.AddScoped<IValidator<UpdateChildDto>, UpdateChildValidator>();
        services.AddScoped<IValidator<AddToCartDto>, AddToCartValidator>();
        services.AddScoped<IValidator<UpdateCartItemQuantityDto>, UpdateCartItemQuantityValidator>();

        services.AddScoped<IValidator<ShipOrderRequestDto>, ShipOrderRequestValidator>();
        services.AddScoped<IValidator<CancelOrderRequestDto>, CancelOrderRequestValidator>();
        services.AddScoped<IValidator<AssignOrderRequestDto>, AssignOrderRequestValidator>();

        services.AddScoped<IValidator<ShipOrderRequestDto>, ShipOrderRequestValidator>();
        services.AddScoped<IValidator<CancelOrderRequestDto>, CancelOrderRequestValidator>();
        services.AddScoped<IValidator<AssignOrderRequestDto>, AssignOrderRequestValidator>();

        services.AddScoped<IValidator<CreateReviewProductDto>, CreateReviewProductValidator>();
        services.AddScoped<IValidator<UpdateReviewProductDto>, UpdateReviewProductValidator>();
        services.AddScoped<IValidator<UpdateModerationStatusDto>, UpdateModerationStatusValidator>();
        services.AddScoped<IValidator<CreateStaffReplyDto>, CreateStaffReplyValidator>();
        services.AddScoped<IValidator<UpdateStaffReplyDto>, UpdateStaffReplyValidator>();
        services.AddScoped<IValidator<CreateWalletRequestDto>, CreateWalletRequestValidator>();
        services.AddScoped<IValidator<VerifyWalletPinRequestDto>, VerifyWalletPinRequestValidator>();
        services.AddScoped<IValidator<CreateSePayTopUpQrRequestDto>, CreateSePayTopUpQrRequestValidator>();
        services.AddScoped<IValidator<ChangeWalletPinRequestDto>, ChangeWalletPinRequestValidator>();
        services.AddScoped<IValidator<VerifyForgotWalletPinOtpRequestDto>, VerifyForgotWalletPinOtpRequestValidator>();
        services.AddScoped<IValidator<ResetForgotWalletPinRequestDto>, ResetForgotWalletPinRequestValidator>();
        services.AddScoped<IValidator<UpdateWalletStatusDto>, UpdateWalletStatusValidator>();

        services.AddScoped<IValidator<CreateShiftTemplateDto>, CreateShiftTemplateValidator>();
        services.AddScoped<IValidator<UpdateShiftTemplateDto>, UpdateShiftTemplateValidator>();
        services.AddScoped<IValidator<CreateWorkScheduleDto>, CreateWorkScheduleValidator>();
        services.AddScoped<IValidator<UpdateWorkScheduleDto>, UpdateWorkScheduleValidator>();
        services.AddScoped<IValidator<AssignQueueOrderRequestDto>, AssignQueueOrderRequestValidator>();
        services.AddScoped<IValidator<ReassignOrderRequestDto>, ReassignOrderRequestValidator>();
        services.AddScoped<IValidator<UpdateShiftCapacityDto>, UpdateShiftCapacityValidator>();

        services.AddScoped<IWorkScheduleShiftRules, WorkScheduleShiftRules>();

        services.AddScoped<IVoucherRepository, VoucherRepository>();
        services.AddScoped<IVoucherService, VoucherService>();

        services.AddScoped<ISuperCategoryRepository, SuperCategoryRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IBlogRepository, BlogRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<ICampaignApprovalLogRepository, CampaignApprovalLogRepository>();
        services.AddScoped<ICampaignScheduleRepository, CampaignScheduleRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<ICustomerChildRepository, CustomerChildRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<IProductFollowerRepository, ProductFollowerRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IRefundImageRepository, RefundImageRepository>();
        services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IShiftTemplateRepository, ShiftTemplateRepository>();
        services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
        services.AddScoped<IStaffShiftCapacityRepository, StaffShiftCapacityRepository>();
        services.AddScoped<IOrderAssignmentRepository, OrderAssignmentRepository>();
        services.AddScoped<IOrderQueueRepository, OrderQueueRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISuperCategoryService, SuperCategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<ICustomerChildService, CustomerChildService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IProductFollowerService, ProductFollowerService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IRefundService, RefundService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IAdminWalletService, AdminWalletService>();
        services.AddScoped<IShiftTemplateService, ShiftTemplateService>();
        services.AddScoped<IWorkScheduleService, WorkScheduleService>();
        services.AddScoped<IShiftAssignmentService, ShiftAssignmentService>();

        // Resolver business object
        services.AddScoped<IBusinessObjectResolver, VoucherResolver>();
        services.AddScoped<IBusinessObjectResolver, ProductResolver>();
        services.AddScoped<IBusinessObjectResolver, BlogPostResolver>();
        services.AddScoped<IBusinessObjectResolver, SaleResolver>();
        services.AddScoped<BusinessObjectResolverFactory>();

        // Renderer template
        services.AddScoped<ITemplateRenderer, TemplateRenderer>();

        services.AddScoped<IRedisService, RedisService>();
        services.TryAddScoped<ICartRealtimeService, NoOpCartRealtimeService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<ILoginAttemptService, LoginAttemptService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICustomerNotificationPreferencesService, CustomerNotificationPreferencesService>();
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IOrderAccessService, OrderAccessService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IShippingWebhookService, ShippingWebhookService>();
        services.AddScoped<IGhnWebhookService, GhnWebhookService>();
        services.AddScoped<IShippingReturnFlowService, ShippingReturnFlowService>();

        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IPromotionService, PromotionService>();

        services.AddScoped<IImageUploadService, CloudinaryImageUploadService>();

        // --- Vouchers ---
        services.Configure<VoucherRiskThresholds>(
            configuration.GetSection("Vouchers"));

        // --- Campaigns ---
        services.Configure<ToyStore.Application.Common.Models.CampaignSettings>(
            configuration.GetSection("Campaigns"));

        // --- GHN van chuyen ---
        services.Configure<GhnOptions>(
            configuration.GetSection(GhnOptions.SectionName));
        services.Configure<ShopAddressOptions>(
            configuration.GetSection(ShopAddressOptions.SectionName));
        services.Configure<AiModerationOptions>(
            configuration.GetSection(AiModerationOptions.SectionName));

        services.AddHttpClient("GHN", (sp, client) =>
        {
            var opts = configuration.GetSection(GhnOptions.SectionName).Get<GhnOptions>()
                       ?? new GhnOptions();
            if (!string.IsNullOrWhiteSpace(opts.ApiEndpoint))
                client.BaseAddress = new Uri(opts.ApiEndpoint.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(opts.ApiToken))
                client.DefaultRequestHeaders.Add("Token", opts.ApiToken);
            if (opts.ShopId > 0)
                client.DefaultRequestHeaders.Add("ShopId", opts.ShopId.ToString());
        });

        services.AddScoped<IGhnClient, GhnClient>();

        services.AddHttpClient("AI_MODERATION", (sp, client) =>
        {
            var opts = configuration.GetSection(AiModerationOptions.SectionName).Get<AiModerationOptions>()
                       ?? new AiModerationOptions();
            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
            {
                client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            }
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds <= 0 ? 10 : opts.TimeoutSeconds);
        });
        services.AddScoped<IBlogCommentModerationGateway, BlogCommentModerationGateway>();

        // --- SE_PAY / VietQR ---
        services.Configure<SePayOptions>(
            configuration.GetSection(SePayOptions.SectionName));
        services.AddScoped<ISePayWebhookService, SePayWebhookService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IOrderLifecycleService, OrderLifecycleService>();
        services.AddScoped<IWalletRefundCreditor, WalletRefundCreditorService>();
        services.AddScoped<IOrderCustomerService, OrderCustomerService>();

        // Cau hinh webhook tokens
        services.Configure<WebhookOptions>(
            configuration.GetSection(WebhookOptions.SectionName));

        // ── Notification system ──────────────────────────────────────────────
        // Application layer: dispatcher, handlers, preference gate
        services.AddApplication();

        // Template renderer (IMemoryCache required)
        services.AddMemoryCache();
        services.AddScoped<INotificationTemplateRenderer, NotificationTemplateRenderer>();

        // Channels — đăng ký cả hai để IEnumerable<INotificationChannel> resolve đúng
        services.AddScoped<INotificationChannel, WebBellChannel>();
        services.AddScoped<INotificationChannel, EmailChannel>();

        // Email sender (SMTP)
        services.AddScoped<IEmailSender, SmtpNotificationEmailSender>();

        // Hub read service (Hub injects this instead of DbContext directly)
        services.AddScoped<INotificationReadService, NotificationReadService>();

        // Outbox event publisher
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        services.AddScoped<IShiftCapacityMonitor, ShiftCapacityMonitor>();

        // Hub push service — default to NoOp; API project overrides with real impl
        services.TryAddScoped<INotificationHubService, NoOpNotificationHubService>();

        services.AddSingleton<ITimeProvider, ToyStore.Infrastructure.Services.TimeProvider>();

        return services;
    }
}

