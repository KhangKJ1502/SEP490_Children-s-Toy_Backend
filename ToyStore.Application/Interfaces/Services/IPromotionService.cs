using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Promotions;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Interface định nghĩa các phương thức xử lý nghiệp vụ cho tính năng Quản lý Khuyến mãi (Promotion), Flash Sale theo khung giờ và giảm giá theo sản phẩm.
/// </summary>
public interface IPromotionService
{
    /// <summary>
    /// Lấy danh sách chương trình khuyến mãi có phân trang, sắp xếp và lọc theo từ khóa tìm kiếm, trạng thái.
    /// </summary>
    /// <param name="pageNumber">Số trang hiện tại (bắt đầu từ 1).</param>
    /// <param name="pageSize">Số lượng bản ghi mỗi trang (1 đến 100).</param>
    /// <param name="sortBy">Tên trường sắp xếp (PromotionName, StartDate, EndDate, Status, CreatedAt,...).</param>
    /// <param name="sortDesc">true để sắp xếp giảm dần, false để tăng dần.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm theo tên hoặc mô tả chương trình.</param>
    /// <param name="status">Trạng thái cần lọc (Scheduled, Active, Inactive, Expired).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa PaginatedResponse danh sách PromotionListDto.</returns>
    Task<Result<PaginatedResponse<PromotionListDto>>> GetPromotionsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các chương trình FLASH_SALE đang hoạt động (Active) hoặc đã lên lịch (Scheduled).
    /// Phục vụ cho giao diện trang chủ/banner phía Client người dùng mà không yêu cầu đăng nhập.
    /// </summary>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách PromotionDto của các chương trình Flash Sale kèm các Time Slot và sản phẩm.</returns>
    Task<Result<List<PromotionDto>>> GetFlashSalePromotionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin chi tiết của một chương trình khuyến mãi theo ID (bao gồm danh sách sản phẩm hoặc khung giờ Flash Sale liên quan).
    /// </summary>
    /// <param name="promotionId">Mã ID của chương trình khuyến mãi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa PromotionDto nếu tìm thấy, hoặc NotFound nếu không tồn tại.</returns>
    Task<Result<PromotionDto>> GetPromotionByIdAsync(
        int promotionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới một chương trình khuyến mãi (NORMAL hoặc FLASH_SALE) cùng danh sách sản phẩm hoặc khung giờ tương ứng trong Transaction an toàn.
    /// </summary>
    /// <param name="request">DTO chứa thông tin tạo chương trình khuyến mãi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa PromotionDto của chương trình vừa tạo thành công.</returns>
    Task<Result<PromotionDto>> CreatePromotionAsync(
        CreatePromotionDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin chương trình khuyến mãi theo ID (hỗ trợ Partial Update và chỉnh sửa cấu trúc danh sách sản phẩm/Time Slot).
    /// Áp dụng các quy tắc kiểm tra trùng lịch, cập nhật giá khuyến mãi cho sản phẩm, và cập nhật trạng thái tự động theo thời gian.
    /// </summary>
    /// <param name="promotionId">Mã ID của chương trình khuyến mãi cần cập nhật.</param>
    /// <param name="request">DTO chứa các trường cần cập nhật.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa PromotionDto chi tiết sau khi cập nhật thành công.</returns>
    Task<Result<PromotionDto>> UpdatePromotionAsync(
        int promotionId,
        UpdatePromotionDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các chương trình khuyến mãi đang có hiệu lực áp dụng trực tiếp cho một sản phẩm cụ thể.
    /// </summary>
    /// <param name="productId">Mã ID của sản phẩm cần tra cứu.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách ProductPromotionInfoDto chứa thông tin giảm giá của sản phẩm.</returns>
    Task<Result<List<ProductPromotionInfoDto>>> GetPromotionsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default);
}
