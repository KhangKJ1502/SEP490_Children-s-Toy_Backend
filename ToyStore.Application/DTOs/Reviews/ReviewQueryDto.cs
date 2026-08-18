namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) chứa các tham số truy vấn tìm kiếm, lọc và phân trang cho danh sách đánh giá công khai của sản phẩm.
/// </summary>
public class ReviewQueryDto
{
    /// <summary>
    /// Mã ID của sản phẩm cần lấy danh sách đánh giá.
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// Số trang cần lấy (bắt đầu từ 1, mặc định 1).
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Số lượng bản ghi trên một trang (mặc định 10, tối đa 100).
    /// </summary>
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Tên trường sắp xếp ("CreatedAt", "Rating").
    /// </summary>
    public string? SortBy { get; set; }
    
    /// <summary>
    /// true: sắp xếp giảm dần (mặc định true), false: sắp xếp tăng dần.
    /// </summary>
    public bool SortDesc { get; set; } = true;
    
    /// <summary>
    /// Lọc đánh giá theo số sao cụ thể (1 đến 5 sao).
    /// </summary>
    public byte? Rating { get; set; }
    
    /// <summary>
    /// Lọc đánh giá có hình ảnh kèm theo hay không (true: có ảnh, false: không ảnh, null: tất cả).
    /// </summary>
    public bool? HasImage { get; set; }
    
    /// <summary>
    /// Từ khóa tìm kiếm xuất hiện trong nội dung bình luận.
    /// </summary>
    public string? SearchTerm { get; set; }
}
