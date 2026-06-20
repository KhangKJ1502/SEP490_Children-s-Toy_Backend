using ToyStore.Application.DTOs.Dashboard;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IAdminDashboardService
{
    Task<Result<DashboardOrderStatusStatisticsDto>> GetOrderStatusStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardNewCustomerStatisticsDto>> GetNewCustomerStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardGrowthStatisticsDto>> GetGrowthStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardTotalProductsDto>> GetTotalProductsAsync(
        CancellationToken cancellationToken = default);
}
