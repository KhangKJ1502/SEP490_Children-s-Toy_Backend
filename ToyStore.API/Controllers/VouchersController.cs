using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Vouchers;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// API quản lý voucher.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _voucherService;
    private readonly ILogger<VouchersController> _logger;

    public VouchersController(IVoucherService voucherService, ILogger<VouchersController> logger)
    {
        _voucherService = voucherService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách voucher có phân trang, tìm kiếm và sắp xếp.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<VoucherListDto>>> GetVouchers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _voucherService.GetVouchersAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Tạo mới voucher.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VoucherDto>> CreateVoucher(
        [FromBody] CreateVoucherDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _voucherService.CreateVoucherAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Created voucher {VoucherId} via API",
                result.Data!.VoucherId);

            return result.ToCreatedResult($"/api/vouchers/{result.Data!.VoucherId}");
        }

        return result.ToActionResult();
    }

    /// <summary>
    /// Cập nhật voucher theo ID.
    /// </summary>
    [HttpPut("{voucherId:int}")]
    public async Task<ActionResult<VoucherDto>> UpdateVoucher(
        int voucherId,
        [FromBody] UpdateVoucherDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _voucherService.UpdateVoucherAsync(voucherId, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy chi tiết voucher theo ID.
    /// </summary>
    [HttpGet("{voucherId:int}")]
    public async Task<ActionResult<VoucherDto>> GetVoucherById(
        int voucherId,
        CancellationToken cancellationToken = default)
    {
        var result = await _voucherService.GetVoucherByIdAsync(voucherId, cancellationToken);
        return result.ToActionResult();
    }

}
