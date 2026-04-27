
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Mappings;
using ToyStore.Application.Validators.Vouchers;
using AutoMapper;
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


                services.AddAutoMapper(_ => { }, typeof(VoucherProfile).Assembly);
                services.AddValidatorsFromAssemblyContaining<CreateVoucherValidator>();

                services.AddScoped<IVoucherRepository, VoucherRepository>();
                services.AddScoped<IVoucherService, VoucherService>();
                services.AddAutoMapper(cfg => { }, typeof(TemplateProfile).Assembly);

                services.AddScoped<ISuperCategoryRepository, SuperCategoryRepository>();
                services.AddScoped<ICategoryRepository, CategoryRepository>();
                services.AddScoped<IAccountRepository, AccountRepository>();
                services.AddScoped<IAccountService, AccountService>();
                services.AddScoped<IBrandRepository, BrandRepository>();
                services.AddScoped<ITemplateRepository, TemplateRepository>();
                services.AddScoped<IUnitOfWork, UnitOfWork>();
                services.AddScoped<ICategoryService, CategoryService>();
                services.AddScoped<IBrandService, BrandService>();
                services.AddScoped<ITemplateService, TemplateService>();

                return services;
        }
}
