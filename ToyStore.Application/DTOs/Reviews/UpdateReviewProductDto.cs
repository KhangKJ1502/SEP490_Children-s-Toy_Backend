using Microsoft.AspNetCore.Http;

namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) nhận dữ liệu từ Client (dạng Multipart Form-Data) để cập nhật hoặc xóa mềm đánh giá sản phẩm.
/// </summary>
public class UpdateReviewProductDto
{
    /// <summary>
    /// Điểm số sao mới (1 đến 5 sao, tùy chọn).
    /// </summary>
    public byte? Rating { get; set; }
    
    /// <summary>
    /// Nội dung bình luận mới (tùy chọn, tối đa 500 ký tự).
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Danh sách các tệp hình ảnh mới tải lên thay thế cho toàn bộ ảnh cũ (tối đa 3 ảnh, mỗi ảnh &lt;= 5MB).
    /// </summary>
    public List<IFormFile>? Images { get; set; }
    
    /// <summary>
    /// Đánh dấu xóa mềm đánh giá (true nếu khách hàng muốn xóa đánh giá của mình).
    /// </summary>
    public bool? IsDeleted { get; set; }
}
