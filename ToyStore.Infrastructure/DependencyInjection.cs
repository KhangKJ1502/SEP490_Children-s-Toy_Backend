
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ToyStore.Application.DTOs.Accounts;
using ToyStore.Application.DTOs.Addresses;
using ToyStore.Application.DTOs.Auth;
using ToyStore.Application.DTOs.Brands;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.DTOs.Carts;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Application.DTOs.Profiles;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Application.DTOs.CustomerChildren;
using ToyStore.Application.DTOs.Templates;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Mappings;
using ToyStore.Application.Validators.Accounts;
using ToyStore.Application.Validators.Addresses;
using ToyStore.Application.Validators.Auth;
using ToyStore.Application.Validators.Brands;
using ToyStore.Application.Validators.Blogs;
using ToyStore.Application.Validators.Campaigns;
using ToyStore.Application.Validators.Carts;
using ToyStore.Application.Validators.Orders;
using ToyStore.Application.Validators.Profiles;
using ToyStore.Application.Validators.Reviews;
using ToyStore.Application.Validators.CustomerChildren;
using ToyStore.Application.Validators.Templates;
using ToyStore.Infrastructure.Data;
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
                configuration.GetConnectionString("DefaultConnection")));

        var redisConnectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false;
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));


        // Đăng ký FluentValidation
        services.AddValidatorsFromAssemblyContaining<Application.Validators.SuperCategories.CreateSuperCategoryValidator>();

        // Đăng ký AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<AccountProfile>();
            cfg.AddProfile<VoucherProfile>();
            cfg.AddProfile<TemplateProfile>();
            cfg.AddProfile<PromotionProfile>();
            cfg.AddProfile<CampaignProfile>();
            cfg.AddProfile<SuperCategoryProfile>();
            cfg.AddProfile<CategoryProfile>();
            cfg.AddProfile<ProductProfile>();
            cfg.AddProfile<BrandProfile>();
            cfg.AddProfile<BlogProfile>();
            cfg.AddProfile<RoleProfile>();
            cfg.AddProfile<AddressProfile>();
            cfg.AddProfile<CartProfile>();
            cfg.AddProfile<OrdersProfile>();
            cfg.AddProfile<ReviewProfile>();
            cfg.AddProfile<CustomerChildProfile>();
        });

        services.AddScoped<IValidator<CreateBrandDto>, CreateBrandValidator>();
        services.AddScoped<IValidator<UpdateBrandDto>, UpdateBrandValidator>();
        services.AddScoped<IValidator<CreateBlogDto>, CreateBlogValidator>();
        services.AddScoped<IValidator<UpdateBlogDto>, UpdateBlogValidator>();
        services.AddScoped<IValidator<SubmitBlogDto>, SubmitBlogValidator>();
        services.AddScoped<IValidator<ApproveBlogDto>, ApproveBlogValidator>();
        services.AddScoped<IValidator<UpdateBlogFeaturedDto>, UpdateBlogFeaturedValidator>();


        services.AddScoped<IValidator<CreateAccountDto>, CreateAccountValidator>();
        services.AddScoped<IValidator<UpdateAccountStatusDto>, UpdateAccountStatusValidator>();
        services.AddScoped<IValidator<CreateTemplateDto>, CreateTemplateValidator>();
        services.AddScoped<IValidator<UpdateTemplateDto>, UpdateTemplateValidator>();
        services.AddScoped<IValidator<CreateCampaignDto>, CreateCampaignValidator>();
        services.AddScoped<IValidator<UpdateProfileDto>, UpdateProfileValidator>();
        services.AddScoped<IValidator<ChangeCustomerPasswordDto>, ChangeCustomerPasswordValidator>();
        services.AddScoped<IValidator<UpdateCampaignDto>, UpdateCampaignValidator>();
        services.AddScoped<IValidator<ChangePasswordDto>, ChangePasswordValidator>();
        services.AddScoped<IValidator<UpdateCampaignDto>, UpdateCampaignValidator>();

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

        services.AddScoped<IVoucherRepository, VoucherRepository>();
        services.AddScoped<IVoucherService, VoucherService>();

        services.AddScoped<ISuperCategoryRepository, SuperCategoryRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IBlogRepository, BlogRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<ICustomerChildRepository, CustomerChildRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
<<<<<<< HEAD
        services.AddScoped<IWishlistRepository, WishlistRepository>();
=======
        services.AddScoped<IReviewRepository, ReviewRepository>();
>>>>>>> 8e31d08c7157223382caaa2f1dbe38480be08287
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
<<<<<<< HEAD
        services.AddScoped<IWishlistService, WishlistService>();
=======
        services.AddScoped<IReviewService, ReviewService>();
>>>>>>> 8e31d08c7157223382caaa2f1dbe38480be08287

        // Resolver business object
        services.AddScoped<IBusinessObjectResolver, VoucherResolver>();
        services.AddScoped<IBusinessObjectResolver, ProductResolver>();
        services.AddScoped<IBusinessObjectResolver, BlogPostResolver>();
        services.AddScoped<IBusinessObjectResolver, SaleResolver>();
        services.AddScoped<BusinessObjectResolverFactory>();

        // Renderer template
        services.AddScoped<ITemplateRenderer, TemplateRenderer>();

        services.AddScoped<IRedisService, RedisService>();
        services.AddScoped<ICartRealtimeService, CartRealtimeService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IShippingWebhookService, ShippingWebhookService>();

        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IPromotionService, PromotionService>();

        services.AddScoped<IImageUploadService, CloudinaryImageUploadService>();

        // --- GHN van chuyen ---
        services.Configure<GhnOptions>(
            configuration.GetSection(GhnOptions.SectionName));
        services.Configure<ShopAddressOptions>(
            configuration.GetSection(ShopAddressOptions.SectionName));

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

        // Cau hinh webhook tokens
        services.Configure<WebhookOptions>(
            configuration.GetSection(WebhookOptions.SectionName));

        return services;
    }
}

