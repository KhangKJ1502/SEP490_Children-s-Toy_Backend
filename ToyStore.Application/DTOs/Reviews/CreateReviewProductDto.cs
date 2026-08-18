using Microsoft.AspNetCore.Http;

namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) nhận dữ liệu từ Client (dạng Multipart Form-Data) để tạo mới một đánh giá sản phẩm.
/// </summary>
public class CreateReviewProductDto
{
    /// <summary>
    /// Mã ID của đơn hàng đã hoàn tất (Completed) mà khách hàng đã mua sản phẩm này.
    /// </summary>
    public int OrderId { get; set; }
    
    /// <summary>
    /// Mã ID của sản phẩm cần gửi đánh giá.
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// Điểm số sao đánh giá sản phẩm (từ 1 đến 5 sao).
    /// </summary>
    public byte Rating { get; set; }
    
    /// <summary>
    /// Nội dung bình luận / nhận xét chi tiết về sản phẩm (tối đa 500 ký tự).
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Danh sách các tệp hình ảnh đính kèm minh họa cho đánh giá (tối đa 3 ảnh, mỗi ảnh &lt;= 5MB).
    /// </summary>
    public List<IFormFile>? Images { get; set; }
}
