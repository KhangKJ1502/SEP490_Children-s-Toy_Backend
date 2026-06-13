using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Customers;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// APIs quản lý customer cho admin.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Staff")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Lấy danh sách customer có phân trang và tìm kiếm.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CustomerListDto>>> GetCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetCustomersAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy chi tiết customer theo account ID.
    /// </summary>
    [HttpGet("{accountId:int}")]
    public async Task<ActionResult<CustomerDetailDto>> GetCustomerById(
        [FromRoute] int accountId,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetCustomerByIdAsync(accountId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Cập nhật customer theo account ID.
    /// </summary>
    [HttpPut("{accountId:int}")]
    public async Task<ActionResult<CustomerDetailDto>> UpdateCustomer(
        [FromRoute] int accountId,
        [FromBody] UpdateCustomerDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.UpdateCustomerAsync(accountId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{accountId:int}/delivery-abuse/block")]
    public async Task<ActionResult<CustomerDetailDto>> BlockCustomerForDeliveryAbuse(
        [FromRoute] int accountId,
        [FromBody] ManualBlockCustomerDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.BlockCustomerForDeliveryAbuseAsync(accountId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
