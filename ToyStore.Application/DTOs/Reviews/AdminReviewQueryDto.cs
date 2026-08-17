namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) chứa các tham số lọc và phân trang phục vụ màn hình tra cứu/kiểm duyệt đánh giá của Admin/Staff.
/// </summary>
public class AdminReviewQueryDto
{
    /// <summary>
    /// Số trang cần lấy (bắt đầu từ 1, mặc định 1).
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Số lượng bản ghi mỗi trang (mặc định 10, tối đa 100).
    /// </summary>
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Tên trường cần sắp xếp (ví dụ: "CreatedAt", "Rating").
    /// </summary>
    public string? SortBy { get; set; }
    
    /// <summary>
    /// true: sắp xếp giảm dần (mặc định true), false: sắp xếp tăng dần.
    /// </summary>
    public bool SortDesc { get; set; } = true;
    
    /// <summary>
    /// Lọc theo trạng thái kiểm duyệt ("Pending", "Approved", "Rejected", "ManualReview", "Hidden").
    /// </summary>
    public string? ModerationStatus { get; set; }
    
    /// <summary>
    /// Lọc theo mã ID sản phẩm cụ thể.
    /// </summary>
    public int? ProductId { get; set; }
    
    /// <summary>
    /// Lọc theo mã ID tài khoản khách hàng viết đánh giá.
    /// </summary>
    public int? AccountId { get; set; }
    
    /// <summary>
    /// Lọc theo mã ID đơn hàng.
    /// </summary>
    public int? OrderId { get; set; }
    
    /// <summary>
    /// Từ khóa tìm kiếm trong nội dung bình luận, tên sản phẩm hoặc tên tài khoản.
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Lọc các đánh giá được tạo từ ngày này trở đi.
    /// </summary>
    public DateTime? FromDate { get; set; }
    
    /// <summary>
    /// Lọc các đánh giá được tạo đến hết ngày này.
    /// </summary>
    public DateTime? ToDate { get; set; }
    
    /// <summary>
    /// Lọc theo cờ xóa mềm (true: đã xóa, false: chưa xóa, null: mặc định chưa xóa).
    /// </summary>
    public bool? IsDeleted { get; set; }
}
