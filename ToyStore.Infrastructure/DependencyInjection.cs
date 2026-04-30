
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ToyStore.Application.DTOs.Accounts;
using ToyStore.Application.DTOs.Brands;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.DTOs.Templates;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Mappings;
using ToyStore.Application.Validators.Accounts;
using ToyStore.Application.Validators.Brands;
using ToyStore.Application.Validators.Campaigns;
using ToyStore.Application.Validators.Templates;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Repositories;
using ToyStore.Infrastructure.Services;


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
        });

        services.AddScoped<IValidator<CreateBrandDto>, CreateBrandValidator>();
        services.AddScoped<IValidator<UpdateBrandDto>, UpdateBrandValidator>();


        services.AddScoped<IValidator<CreateAccountDto>, CreateAccountValidator>();
        services.AddScoped<IValidator<UpdateAccountStatusDto>, UpdateAccountStatusValidator>();
        services.AddScoped<IValidator<CreateTemplateDto>, CreateTemplateValidator>();
        services.AddScoped<IValidator<UpdateTemplateDto>, UpdateTemplateValidator>();
        services.AddScoped<IValidator<CreateCampaignDto>, CreateCampaignValidator>();


        services.AddScoped<IVoucherRepository, VoucherRepository>();
        services.AddScoped<IVoucherService, VoucherService>();

        services.AddScoped<ISuperCategoryRepository, SuperCategoryRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISuperCategoryService, SuperCategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<ICampaignService, CampaignService>();

        services.AddScoped<IRedisService, RedisService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IPromotionService, PromotionService>();

        return services;
    }
}

