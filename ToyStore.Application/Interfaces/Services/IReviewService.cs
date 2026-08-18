using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Interface định nghĩa các phương thức xử lý nghiệp vụ cho tính năng Đánh giá sản phẩm (Product Review).
/// Phục vụ cả 2 đối tượng người dùng: Khách hàng (tạo, sửa, xem, like đánh giá) và Quản trị/Nhân viên (kiểm duyệt, phản hồi).
/// </summary>
public interface IReviewService
{
    // ==========================================
    // Public / Customer Endpoints
    // ==========================================

    /// <summary>
    /// Lấy danh sách đánh giá công khai (chỉ lấy trạng thái APPROVED) của một sản phẩm, hỗ trợ lọc sao, lọc có ảnh, sắp xếp và phân trang.
    /// </summary>
    /// <param name="query">DTO chứa các tham số truy vấn tìm kiếm và lọc.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa PaginatedResponse danh sách ReviewProductListDto.</returns>
    Task<Result<PaginatedResponse<ReviewProductListDto>>> GetPublicListAsync(
        ReviewQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Khách hàng tạo đánh giá mới cho sản phẩm trong đơn hàng đã hoàn tất (COMPLETED).
    /// Hỗ trợ upload ảnh lên Cloudinary và chạy kiểm duyệt nội dung tự động (Auto-moderation).
    /// </summary>
    /// <param name="dto">DTO chứa thông tin đánh giá và danh sách file ảnh tải lên.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa ReviewProductDto sau khi tạo thành công.</returns>
    Task<Result<ReviewProductDto>> CreateReviewAsync(
        CreateReviewProductDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Khách hàng chỉnh sửa đánh giá (chỉ được sửa 1 lần duy nhất trong vòng 3 ngày kể từ khi tạo).
    /// </summary>
    /// <param name="reviewId">Mã ID của đánh giá cần chỉnh sửa.</param>
    /// <param name="dto">DTO chứa dữ liệu chỉnh sửa, danh sách ID ảnh giữ lại và ảnh mới tải lên.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa ReviewProductDto sau khi cập nhật thành công.</returns>
    Task<Result<ReviewProductDto>> UpdateReviewAsync(
        int reviewId, UpdateReviewProductDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các sản phẩm từ đơn hàng đã hoàn tất mà khách hàng hiện tại chưa gửi đánh giá.
    /// </summary>
    /// <param name="pageNumber">Số trang cần lấy.</param>
    /// <param name="pageSize">Số lượng bản ghi mỗi trang.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa PaginatedResponse danh sách UnreviewedProductDto.</returns>
    Task<Result<PaginatedResponse<UnreviewedProductDto>>> GetUnreviewedProductsAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách toàn bộ đánh giá của khách hàng đang đăng nhập kèm trạng thái kiểm duyệt (APPROVED, PENDING_APPROVAL, REJECTED, HIDDEN).
    /// </summary>
    /// <param name="query">DTO lọc theo rating, trạng thái kiểm duyệt, khoảng thời gian và phân trang.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa PaginatedResponse danh sách MyReviewDto.</returns>
    Task<Result<PaginatedResponse<MyReviewDto>>> GetMyReviewsAsync(
        MyReviewQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Khách hàng Toggle Like / Bỏ Like cho một đánh giá sản phẩm.
    /// </summary>
    /// <param name="reviewId">Mã ID của đánh giá sản phẩm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa ReviewLikeResponseDto.</returns>
    Task<Result<ReviewLikeResponseDto>> ToggleLikeAsync(int reviewId, CancellationToken cancellationToken = default);

    // ==========================================
    // Admin / Staff Moderation Endpoints
    // ==========================================

    /// <summary>
    /// Admin/Staff lấy danh sách đánh giá sản phẩm toàn hệ thống có phân trang và bộ lọc nâng cao.
    /// </summary>
    /// <param name="query">DTO chứa các tiêu chí lọc: trạng thái kiểm duyệt, số sao, từ khóa, khoảng ngày.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa PaginatedResponse danh sách AdminReviewListDto.</returns>
    Task<Result<PaginatedResponse<AdminReviewListDto>>> GetAdminListAsync(
        AdminReviewQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin/Staff xem chi tiết một đánh giá sản phẩm bao gồm thông tin đơn hàng, hình ảnh, lịch sử log kiểm duyệt và phản hồi của nhân viên.
    /// </summary>
    /// <param name="reviewId">Mã ID của đánh giá.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa AdminReviewDetailDto.</returns>
    Task<Result<AdminReviewDetailDto>> GetAdminDetailAsync(
        int reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin/Staff cập nhật trạng thái kiểm duyệt thủ công cho đánh giá (APPROVED, REJECTED, HIDDEN) kèm lý do và ghi nhận log kiểm duyệt.
    /// </summary>
    /// <param name="reviewId">Mã ID của đánh giá cần duyệt.</param>
    /// <param name="dto">DTO chứa trạng thái mới và lý do.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa AdminReviewDetailDto sau khi cập nhật.</returns>
    Task<Result<AdminReviewDetailDto>> UpdateModerationStatusAsync(
        int reviewId, UpdateModerationStatusDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin/Staff tạo phản hồi chính thức từ cửa hàng cho một đánh giá của khách hàng.
    /// </summary>
    /// <param name="reviewId">Mã ID của đánh giá cần phản hồi.</param>
    /// <param name="dto">DTO chứa nội dung phản hồi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa StaffReplyDto.</returns>
    Task<Result<StaffReplyDto>> CreateReplyAsync(
        int reviewId, CreateStaffReplyDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin/Staff chỉnh sửa nội dung phản hồi đã tạo trước đó.
    /// </summary>
    /// <param name="reviewId">Mã ID của đánh giá.</param>
    /// <param name="replyId">Mã ID của bản ghi phản hồi.</param>
    /// <param name="dto">DTO chứa nội dung phản hồi mới.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa StaffReplyDto sau khi cập nhật.</returns>
    Task<Result<StaffReplyDto>> UpdateReplyAsync(
        int reviewId, int replyId, UpdateStaffReplyDto dto, CancellationToken cancellationToken = default);
}
