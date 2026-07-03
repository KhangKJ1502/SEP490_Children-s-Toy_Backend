using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.DTOs.Products;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class RefundsController : ControllerBase
{
    private readonly IRefundService _refundService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IImageUploadService _imageUploadService;

    public RefundsController(
        IRefundService refundService, 
        ICurrentUserService currentUserService,
        IImageUploadService imageUploadService)
    {
        _refundService = refundService;
        _currentUserService = currentUserService;
        _imageUploadService = imageUploadService;
    }

    [HttpGet("reasons")]
    public async Task<ActionResult<ApiResponse<List<RefundReasonDto>>>> GetRefundReasons(CancellationToken cancellationToken = default)
    {
        var reasons = await _refundService.GetRefundReasonsAsync(cancellationToken);
        return Ok(ApiResponse<List<RefundReasonDto>>.Ok(reasons));
    }

    [HttpPost]
    public async Task<ActionResult<RefundDto>> CreateRefund(
        [FromBody] CreateRefundDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.CreateRefundAsync(_currentUserService.AccountId, dto, cancellationToken);
        return result.ToCreatedResult($"api/refunds/{result.Data?.RefundId}");
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RefundListDto>>> GetRefunds(
        [FromQuery] RefundFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.GetRefundsAsync(_currentUserService.AccountId, filter, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<RefundListDto>>.Ok(result));
    }

    [HttpGet("{refundId:int}")]
    public async Task<ActionResult<RefundDto>> GetRefundById(
        [FromRoute] int refundId,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.GetRefundByIdAsync(_currentUserService.AccountId, refundId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{refundId:int}/cancel")]
    public async Task<ActionResult<RefundDto>> CancelRefund(
        [FromRoute] int refundId,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.CancelRefundAsync(_currentUserService.AccountId, refundId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{refundId:int}/pay-return-fee")]
    public async Task<ActionResult<RefundDto>> PayReturnFee(
        [FromRoute] int refundId,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.PayReturnFeeAsync(_currentUserService.AccountId, refundId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadImageResponseDto>> UploadImage(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was provided." });
        }

        using var stream = file.OpenReadStream();
        var result = await _imageUploadService.UploadImageToFolderAsync(stream, file.FileName, "SEP490_Refunds", cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new UploadImageResponseDto { Url = result.Data! });
    }
}
