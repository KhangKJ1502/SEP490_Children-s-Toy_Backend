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

/// <summary>
/// Controller xử lý các yêu cầu Hoàn tiền / Đổi trả từ phía Khách hàng (Customer).
/// Bao gồm: lấy danh mục lý do hoàn tiền, tạo yêu cầu hoàn tiền, xem danh sách và chi tiết yêu cầu,
/// hủy yêu cầu, thanh toán phí vận chuyển hoàn trả và upload hình ảnh bằng chứng hoàn tiền.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class RefundsController : ControllerBase
{
    private readonly IRefundService _refundService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IImageUploadService _imageUploadService;

    /// <summary>
    /// Khởi tạo RefundsController với các dịch vụ cần thiết.
    /// </summary>
    public RefundsController(
        IRefundService refundService, 
        ICurrentUserService currentUserService,
        IImageUploadService imageUploadService)
    {
        _refundService = refundService;
        _currentUserService = currentUserService;
        _imageUploadService = imageUploadService;
    }

    /// <summary>
    /// Lấy danh sách các lý do hoàn tiền/trả hàng được hệ thống hỗ trợ.
    /// </summary>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách lý do hoàn tiền (RefundReasonDto).</returns>
    [HttpGet("reasons")]
    public async Task<ActionResult<ApiResponse<List<RefundReasonDto>>>> GetRefundReasons(CancellationToken cancellationToken = default)
    {
        var reasons = await _refundService.GetRefundReasonsAsync(cancellationToken);
        return Ok(ApiResponse<List<RefundReasonDto>>.Ok(reasons));
    }

    /// <summary>
    /// Tạo một yêu cầu hoàn tiền / đổi trả mới cho đơn hàng đã mua.
    /// </summary>
    /// <param name="dto">Thông tin yêu cầu hoàn tiền (mã đơn hàng, danh sách sản phẩm, lý do, hình ảnh bằng chứng,...).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin chi tiết yêu cầu hoàn tiền vừa tạo.</returns>
    [HttpPost]
    public async Task<ActionResult<RefundDto>> CreateRefund(
        [FromBody] CreateRefundDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.CreateRefundAsync(_currentUserService.AccountId, dto, cancellationToken);
        return result.ToCreatedResult($"api/refunds/{result.Data?.RefundId}");
    }

    /// <summary>
    /// Lấy danh sách các yêu cầu hoàn tiền của khách hàng hiện tại có phân trang và bộ lọc trạng thái.
    /// </summary>
    /// <param name="filter">Bộ lọc danh sách yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách yêu cầu hoàn tiền dạng phân trang (PaginatedResponse của RefundListDto).</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RefundListDto>>> GetRefunds(
        [FromQuery] RefundFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.GetRefundsAsync(_currentUserService.AccountId, filter, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<RefundListDto>>.Ok(result));
    }

    /// <summary>
    /// Xem thông tin chi tiết một yêu cầu hoàn tiền của khách hàng theo mã ID.
    /// </summary>
    /// <param name="refundId">Mã ID của yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Chi tiết yêu cầu hoàn tiền (RefundDto).</returns>
    [HttpGet("{refundId:int}")]
    public async Task<ActionResult<RefundDto>> GetRefundById(
        [FromRoute] int refundId,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.GetRefundByIdAsync(_currentUserService.AccountId, refundId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Khách hàng chủ động hủy yêu cầu hoàn tiền khi yêu cầu đang ở trạng thái chờ xử lý (Pending).
    /// </summary>
    /// <param name="refundId">Mã ID của yêu cầu hoàn tiền cần hủy.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin yêu cầu hoàn tiền sau khi đã cập nhật trạng thái hủy.</returns>
    [HttpPost("{refundId:int}/cancel")]
    public async Task<ActionResult<RefundDto>> CancelRefund(
        [FromRoute] int refundId,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.CancelRefundAsync(_currentUserService.AccountId, refundId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Khách hàng thanh toán phí vận chuyển trả hàng (khi bên chịu trách nhiệm phí trả hàng là Khách hàng).
    /// </summary>
    /// <param name="refundId">Mã ID của yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin yêu cầu hoàn tiền sau khi thanh toán phí trả hàng.</returns>
    [HttpPost("{refundId:int}/pay-return-fee")]
    public async Task<ActionResult<RefundDto>> PayReturnFee(
        [FromRoute] int refundId,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.PayReturnFeeAsync(_currentUserService.AccountId, refundId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Tải lên hình ảnh bằng chứng (sản phẩm lỗi, hư hỏng, vỡ, sai hàng,...) phục vụ tạo yêu cầu hoàn tiền.
    /// </summary>
    /// <param name="file">Tệp hình ảnh cần tải lên Cloudinary.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>URL hình ảnh vừa tải lên.</returns>
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
