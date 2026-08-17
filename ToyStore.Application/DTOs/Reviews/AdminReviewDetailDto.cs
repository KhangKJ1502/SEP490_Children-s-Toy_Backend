namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) chứa toàn bộ chi tiết của một đánh giá sản phẩm dành cho giao diện Quản trị viên/Nhân viên (Admin/Staff).
/// Bao gồm thông tin tài khoản đánh giá, thông tin đơn hàng, sản phẩm, hình ảnh, phản hồi của nhân viên và lịch sử kiểm duyệt.
/// </summary>
public class AdminReviewDetailDto
{
    /// <summary>
    /// Mã ID định danh của đánh giá.
    /// </summary>
    public int ReviewId { get; set; }
    
    /// <summary>
    /// Mã ID tài khoản khách hàng đã viết đánh giá.
    /// </summary>
    public int AccountId { get; set; }
    
    /// <summary>
    /// Tên hiển thị của khách hàng.
    /// </summary>
    public string AccountName { get; set; } = null!;
    
    /// <summary>
    /// Email đăng ký của khách hàng.
    /// </summary>
    public string AccountEmail { get; set; } = null!;
    
    /// <summary>
    /// Mã ID của sản phẩm được đánh giá.
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// Tên của sản phẩm.
    /// </summary>
    public string ProductName { get; set; } = null!;
    
    /// <summary>
    /// Mã ID của đơn hàng liên quan.
    /// </summary>
    public int OrderId { get; set; }
    
    /// <summary>
    /// Mã code hiển thị của đơn hàng (ví dụ: "ORD-2026-001").
    /// </summary>
    public string OrderCode { get; set; } = null!;
    
    /// <summary>
    /// Điểm đánh giá số sao (1 đến 5).
    /// </summary>
    public byte Rating { get; set; }
    
    /// <summary>
    /// Nội dung bình luận chi tiết.
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Trạng thái kiểm duyệt hiện tại ("Pending", "Approved", "Rejected", "ManualReview", "Hidden").
    /// </summary>
    public string ModerationStatus { get; set; } = null!;
    
    /// <summary>
    /// Cờ đánh dấu đã bị xóa mềm hay chưa.
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// Cờ đánh dấu đánh giá này đã từng được khách hàng chỉnh sửa hay chưa.
    /// </summary>
    public bool IsEdited { get; set; }
    
    /// <summary>
    /// Thời điểm tạo đánh giá (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thời điểm cập nhật đánh giá gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Danh sách toàn bộ hình ảnh đính kèm trong đánh giá.
    /// </summary>
    public List<ReviewImageDto> Images { get; set; } = new();
    
    /// <summary>
    /// Danh sách các phản hồi từ phía nhân viên cửa hàng.
    /// </summary>
    public List<StaffReplyDto> Replies { get; set; } = new();
    
    /// <summary>
    /// Danh sách toàn bộ lịch sử kiểm duyệt (AI và Staff) được sắp xếp theo thời gian mới nhất.
    /// </summary>
    public List<ModerationLogDto> ModerationLogs { get; set; } = new();
}
