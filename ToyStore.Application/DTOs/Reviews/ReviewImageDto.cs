namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) chứa thông tin hình ảnh đính kèm của một đánh giá sản phẩm.
/// </summary>
public class ReviewImageDto
{
    /// <summary>
    /// Mã ID định danh của hình ảnh đánh giá.
    /// </summary>
    public int ReviewProductImageId { get; set; }
    
    /// <summary>
    /// Đường dẫn URL hình ảnh (lưu trên Cloudinary).
    /// </summary>
    public string ImageUrl { get; set; } = null!;
}
