using ToyStore.Application.DTOs.Dashboard;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IAdminDashboardService
{
    Task<Result<DashboardRevenueStatisticsDto>> GetRevenueStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardOrderStatusStatisticsDto>> GetOrderStatusStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardNewCustomerStatisticsDto>> GetNewCustomerStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardCompletedOrderStatisticsDto>> GetCompletedOrderStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardGrowthStatisticsDto>> GetGrowthStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardOrderRateStatisticsDto>> GetOrderRateStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardTopSellingProductsDto>> GetTopSellingProductsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardSlowMovingProductsDto>> GetSlowMovingProductsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<Result<DashboardTotalProductsDto>> GetTotalProductsAsync(
        CancellationToken cancellationToken = default);
}
