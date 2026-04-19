using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToyStore.Infrastructure.Data;

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

        return services;
    }
}
