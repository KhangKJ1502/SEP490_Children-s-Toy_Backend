using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Dashboard;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[Authorize(Policy = "Orders.Admin")]
[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("orders-by-status")]
    public async Task<ActionResult<DashboardOrderStatusStatisticsDto>> GetOrderStatusStatistics(
        [FromQuery] DashboardTimeFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetOrderStatusStatisticsAsync(filter, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("new-customers")]
    public async Task<ActionResult<DashboardNewCustomerStatisticsDto>> GetNewCustomerStatistics(
        [FromQuery] DashboardTimeFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetNewCustomerStatisticsAsync(filter, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("completed-orders")]
    public async Task<ActionResult<DashboardCompletedOrderStatisticsDto>> GetCompletedOrderStatistics(
        [FromQuery] DashboardTimeFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetCompletedOrderStatisticsAsync(filter, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("growth")]
    public async Task<ActionResult<DashboardGrowthStatisticsDto>> GetGrowthStatistics(
        [FromQuery] DashboardTimeFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetGrowthStatisticsAsync(filter, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("order-rates")]
    public async Task<ActionResult<DashboardOrderRateStatisticsDto>> GetOrderRateStatistics(
        [FromQuery] DashboardTimeFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetOrderRateStatisticsAsync(filter, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("products/total-count")]
    public async Task<ActionResult<DashboardTotalProductsDto>> GetTotalProducts(
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetTotalProductsAsync(cancellationToken);
        return result.ToActionResult();
    }
}
