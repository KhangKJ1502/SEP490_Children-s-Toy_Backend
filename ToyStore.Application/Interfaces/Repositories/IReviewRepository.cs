using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Interface định nghĩa các phương thức thao tác cơ sở dữ liệu cho Đánh giá sản phẩm (ReviewProduct), hình ảnh, phản hồi của nhân viên, kiểm duyệt và tương tác Like.
/// </summary>
public interface IReviewRepository
{
    // ==========================================
    // Public / Customer Queries
    // ==========================================

    /// <summary>
    /// Lấy danh sách đánh giá công khai đã được duyệt (APPROVED, không bị xóa mềm) của một sản phẩm có phân trang và bộ lọc.
    /// </summary>
    /// <param name="productId">Mã ID sản phẩm.</param>
    /// <param name="pageNumber">Số trang cần lấy.</param>
    /// <param name="pageSize">Số bản ghi mỗi trang.</param>
    /// <param name="sortBy">Tên trường sắp xếp ("CreatedAt", "Rating", "LikeCount").</param>
    /// <param name="sortDesc">true: giảm dần, false: tăng dần.</param>
    /// <param name="rating">Lọc theo số sao cụ thể (1-5 sao).</param>
    /// <param name="hasImage">Lọc đánh giá có hình ảnh hay không.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm trong nội dung bình luận.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách thực thể ReviewProduct kèm hình ảnh, tài khoản và phản hồi nhân viên.</returns>
    Task<List<ReviewProduct>> GetPublicPagedAsync(
        int productId,
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool sortDesc,
        byte? rating,
        bool? hasImage,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số lượng đánh giá công khai thỏa mãn điều kiện lọc của một sản phẩm.
    /// </summary>
    /// <param name="productId">Mã ID sản phẩm.</param>
    /// <param name="rating">Lọc theo số sao.</param>
    /// <param name="hasImage">Lọc theo có hình ảnh.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Tổng số lượng bản ghi thỏa mãn.</returns>
    Task<int> GetPublicCountAsync(
        int productId,
        byte? rating,
        bool? hasImage,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết một đánh giá công khai theo ID (phải có trạng thái APPROVED và chưa bị xóa mềm).
    /// </summary>
    /// <param name="reviewId">Mã ID đánh giá.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể ReviewProduct hoặc null nếu không tìm thấy.</returns>
    Task<ReviewProduct?> GetByIdPublicAsync(int reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thực thể đánh giá kèm danh sách hình ảnh để phục vụ chỉnh sửa.
    /// </summary>
    /// <param name="reviewId">Mã ID đánh giá.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể ReviewProduct hoặc null.</returns>
    Task<ReviewProduct?> GetByIdForUpdateAsync(int reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra xem khách hàng đã từng đánh giá sản phẩm trong đơn hàng cụ thể hay chưa (tránh trùng lặp đánh giá).
    /// </summary>
    /// <param name="accountId">Mã ID tài khoản khách hàng.</param>
    /// <param name="orderId">Mã ID đơn hàng.</param>
    /// <param name="productId">Mã ID sản phẩm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>true nếu đã tồn tại đánh giá, ngược lại false.</returns>
    Task<bool> ExistsByAccountOrderProductAsync(
        int accountId, 
        int orderId, 
        int productId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin đơn hàng và chi tiết đơn hàng (Order & OrderDetails) của khách hàng để kiểm tra tính hợp lệ trước khi cho phép đánh giá.
    /// </summary>
    /// <param name="orderId">Mã ID đơn hàng.</param>
    /// <param name="accountId">Mã ID tài khoản khách hàng.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể Order hoặc null.</returns>
    Task<Order?> GetOrderForReviewAsync(
        int orderId, 
        int accountId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm mới một bản ghi đánh giá sản phẩm vào DbContext.
    /// </summary>
    /// <param name="review">Thực thể ReviewProduct.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    Task AddReviewAsync(ReviewProduct review, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm mới một bản ghi hình ảnh đính kèm của đánh giá.
    /// </summary>
    /// <param name="image">Thực thể ReviewProductImage.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    Task AddImageAsync(ReviewProductImage image, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm mới một bản ghi nhật ký kiểm duyệt đánh giá (ReviewModerationLog).
    /// </summary>
    /// <param name="log">Thực thể ReviewModerationLog.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    Task AddModerationLogAsync(ReviewModerationLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu cập nhật thực thể ReviewProduct trong DbContext.
    /// </summary>
    /// <param name="review">Thực thể ReviewProduct.</param>
    void Update(ReviewProduct review);

    // ==========================================
    // Customer My Reviews Queries
    // ==========================================

    /// <summary>
    /// Lấy danh sách các dòng chi tiết đơn hàng (OrderDetail) từ các đơn hàng COMPLETED của khách hàng mà chưa có bản ghi đánh giá.
    /// </summary>
    /// <param name="accountId">Mã ID tài khoản khách hàng.</param>
    /// <param name="pageNumber">Số trang cần lấy.</param>
    /// <param name="pageSize">Số bản ghi mỗi trang.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách thực thể OrderDetail kèm thông tin sản phẩm và đơn hàng.</returns>
    Task<List<OrderDetail>> GetUnreviewedProductsAsync(int accountId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số sản phẩm chưa được đánh giá của khách hàng.
    /// </summary>
    /// <param name="accountId">Mã ID tài khoản khách hàng.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Tổng số sản phẩm chưa đánh giá.</returns>
    Task<int> GetUnreviewedProductsCountAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách tất cả các đánh giá mà khách hàng hiện tại đã viết (kèm hình ảnh, phản hồi staff, sản phẩm).
    /// </summary>
    /// <param name="accountId">Mã ID tài khoản khách hàng.</param>
    /// <param name="pageNumber">Số trang cần lấy.</param>
    /// <param name="pageSize">Số bản ghi mỗi trang.</param>
    /// <param name="sortBy">Trường sắp xếp.</param>
    /// <param name="sortDesc">true: giảm dần, false: tăng dần.</param>
    /// <param name="moderationStatus">Lọc theo trạng thái kiểm duyệt.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách thực thể ReviewProduct.</returns>
    Task<List<ReviewProduct>> GetMyReviewsPagedAsync(
        int accountId,
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool sortDesc,
        string? moderationStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số đánh giá của khách hàng thỏa mãn bộ lọc.
    /// </summary>
    /// <param name="accountId">Mã ID tài khoản khách hàng.</param>
    /// <param name="moderationStatus">Trạng thái kiểm duyệt cần lọc.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Tổng số lượng bản ghi.</returns>
    Task<int> GetMyReviewsCountAsync(
        int accountId,
        string? moderationStatus,
        CancellationToken cancellationToken = default);

    // ==========================================
    // Admin / Staff Queries & Actions
    // ==========================================

    /// <summary>
    /// Lấy danh sách toàn bộ đánh giá trong hệ thống dành cho Admin/Staff quản trị với đầy đủ các bộ lọc nâng cao.
    /// </summary>
    /// <param name="pageNumber">Số trang cần lấy.</param>
    /// <param name="pageSize">Số bản ghi mỗi trang.</param>
    /// <param name="sortBy">Trường sắp xếp.</param>
    /// <param name="sortDesc">true: giảm dần, false: tăng dần.</param>
    /// <param name="moderationStatus">Lọc theo trạng thái kiểm duyệt (PENDING_APPROVAL, APPROVED, REJECTED, HIDDEN).</param>
    /// <param name="productId">Lọc theo ID sản phẩm.</param>
    /// <param name="accountId">Lọc theo ID khách hàng.</param>
    /// <param name="orderId">Lọc theo ID đơn hàng.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm.</param>
    /// <param name="fromDate">Từ ngày.</param>
    /// <param name="toDate">Đến ngày.</param>
    /// <param name="isDeleted">Lọc theo cờ xóa mềm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách thực thể ReviewProduct.</returns>
    Task<List<ReviewProduct>> GetAdminPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool sortDesc,
        string? moderationStatus,
        int? productId,
        int? accountId,
        int? orderId,
        string? searchTerm,
        DateTime? fromDate,
        DateTime? toDate,
        bool? isDeleted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số đánh giá thỏa mãn điều kiện lọc của Admin/Staff.
    /// </summary>
    /// <param name="moderationStatus">Trạng thái kiểm duyệt.</param>
    /// <param name="productId">ID sản phẩm.</param>
    /// <param name="accountId">ID khách hàng.</param>
    /// <param name="orderId">ID đơn hàng.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm.</param>
    /// <param name="fromDate">Từ ngày.</param>
    /// <param name="toDate">Đến ngày.</param>
    /// <param name="isDeleted">Cờ xóa mềm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Tổng số bản ghi.</returns>
    Task<int> GetAdminCountAsync(
        string? moderationStatus,
        int? productId,
        int? accountId,
        int? orderId,
        string? searchTerm,
        DateTime? fromDate,
        DateTime? toDate,
        bool? isDeleted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết toàn diện của một đánh giá cho Admin/Staff (kèm hình ảnh, đơn hàng, khách hàng, sản phẩm, log kiểm duyệt, phản hồi staff).
    /// </summary>
    /// <param name="reviewId">Mã ID đánh giá.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể ReviewProduct hoặc null.</returns>
    Task<ReviewProduct?> GetByIdForAdminAsync(int reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy phản hồi của nhân viên theo ID phản hồi.
    /// </summary>
    /// <param name="replyId">Mã ID bản ghi phản hồi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể StaffReviewProductReply hoặc null.</returns>
    Task<StaffReviewProductReply?> GetReplyByIdAsync(int replyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm mới bản ghi phản hồi của nhân viên vào DbContext.
    /// </summary>
    /// <param name="reply">Thực thể StaffReviewProductReply.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    Task AddReplyAsync(StaffReviewProductReply reply, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy bản ghi cảm xúc (Reaction / Like) của một khách hàng trên một đánh giá cụ thể.
    /// </summary>
    /// <param name="reviewId">Mã ID đánh giá.</param>
    /// <param name="accountId">Mã ID tài khoản khách hàng.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể ReviewProductReaction hoặc null.</returns>
    Task<ReviewProductReaction?> GetReactionAsync(int reviewId, int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm mới bản ghi cảm xúc (Reaction) của khách hàng vào DbContext.
    /// </summary>
    /// <param name="reaction">Thực thể ReviewProductReaction.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    Task AddReactionAsync(ReviewProductReaction reaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số lượng Like (ReactionType = LIKE và IsDeleted = false) của một đánh giá.
    /// </summary>
    /// <param name="reviewId">Mã ID đánh giá.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Tổng số lượt like.</returns>
    Task<int> GetLikeCountAsync(int reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy loại Reaction theo mã code (ví dụ: "LIKE").
    /// </summary>
    /// <param name="code">Mã loại cảm xúc.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể ReactionType hoặc null.</returns>
    Task<ReactionType?> GetReactionTypeByCodeAsync(string code, CancellationToken cancellationToken = default);
}
