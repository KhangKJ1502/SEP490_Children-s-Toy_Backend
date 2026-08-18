using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Interface định nghĩa các phương thức thao tác dữ liệu (CRUD, truy vấn phân trang, kiểm tra trùng lặp, Flash Sale) cho thực thể Promotion.
/// </summary>
public interface IPromotionRepository
{
    /// <summary>
    /// Lấy danh sách chương trình khuyến mãi có phân trang, sắp xếp và lọc theo từ khóa, trạng thái (bỏ qua các bản ghi đã xóa mềm).
    /// </summary>
    /// <param name="pageNumber">Số trang cần lấy (bắt đầu từ 1).</param>
    /// <param name="pageSize">Số lượng bản ghi mỗi trang.</param>
    /// <param name="sortBy">Tên trường cần sắp xếp.</param>
    /// <param name="sortDesc">true để sắp xếp giảm dần, false để tăng dần.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm theo tên hoặc mô tả chương trình.</param>
    /// <param name="status">Trạng thái chương trình cần lọc.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng PaginatedResponse chứa danh sách thực thể Promotion và tổng số bản ghi.</returns>
    Task<PaginatedResponse<Promotion>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thực thể chương trình khuyến mãi theo ID, hỗ trợ nạp kèm (Eager Loading) các bảng liên quan như ProductPromotions, PromotionTimeSlots.
    /// </summary>
    /// <param name="promotionId">Mã ID của chương trình khuyến mãi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <param name="includeProperties">Chuỗi tên các navigation property cần nạp kèm (phân cách bằng dấu phẩy).</param>
    /// <returns>Thực thể Promotion nếu tìm thấy, hoặc null nếu không tồn tại hoặc đã bị xóa mềm.</returns>
    Task<Promotion?> GetByIdAsync(int promotionId, CancellationToken cancellationToken = default, string? includeProperties = null);

    /// <summary>
    /// Kiểm tra xem tên chương trình khuyến mãi đã tồn tại hay chưa (không phân biệt hoa thường, bỏ qua bản ghi đã xóa mềm).
    /// Hỗ trợ loại trừ một promotionId cụ thể khi kiểm tra trong luồng cập nhật.
    /// </summary>
    /// <param name="promotionName">Tên chương trình khuyến mãi cần kiểm tra.</param>
    /// <param name="excludePromotionId">Mã ID cần loại trừ khi cập nhật.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>true nếu tên đã tồn tại, false nếu chưa.</returns>
    Task<bool> ExistsPromotionNameAsync(
        string promotionName,
        int? excludePromotionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm một thực thể chương trình khuyến mãi mới vào DbContext.
    /// </summary>
    /// <param name="promotion">Thực thể Promotion cần thêm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu thực thể Promotion đã bị thay đổi để chuẩn bị lưu xuống database.
    /// </summary>
    /// <param name="promotion">Thực thể Promotion cần cập nhật.</param>
    void Update(Promotion promotion);

    /// <summary>
    /// Xoá một thực thể liên kết sản phẩm khuyến mãi (ProductPromotion) khỏi DbContext.
    /// </summary>
    /// <param name="productPromotion">Thực thể ProductPromotion cần xóa.</param>
    void RemoveProductPromotion(ProductPromotion productPromotion);

    /// <summary>
    /// Xoá một thực thể khung giờ Flash Sale (PromotionTimeSlot) khỏi DbContext.
    /// </summary>
    /// <param name="promotionTimeSlot">Thực thể PromotionTimeSlot cần xóa.</param>
    void RemovePromotionTimeSlot(PromotionTimeSlot promotionTimeSlot);

    /// <summary>
    /// Kiểm tra xem một sản phẩm hiện tại có đang nằm trong chương trình khuyến mãi nào đang Active hoặc Scheduled hay không (tránh trùng lịch giảm giá).
    /// </summary>
    /// <param name="productId">Mã ID sản phẩm cần kiểm tra.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>true nếu sản phẩm đang tham gia khuyến mãi hiệu lực, ngược lại false.</returns>
    Task<bool> IsProductInActivePromotionAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các chương trình khuyến mãi đang áp dụng giảm giá trực tiếp cho một sản phẩm cụ thể.
    /// </summary>
    /// <param name="productId">Mã ID sản phẩm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách ProductPromotionInfoDto chứa thông tin mức giảm và chương trình áp dụng.</returns>
    Task<List<DTOs.Promotions.ProductPromotionInfoDto>> GetPromotionsByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách chương trình FLASH_SALE đang Active hoặc Scheduled trong khoảng ngày hiển thị (visibilityDays), bao gồm đầy đủ khung giờ và sản phẩm.
    /// </summary>
    /// <param name="visibilityDays">Số ngày giới hạn hiển thị trước (mặc định là 2 ngày).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách thực thể Promotion thuộc loại Flash Sale.</returns>
    Task<List<Promotion>> GetFlashSalePromotionsAsync(int visibilityDays = 2, CancellationToken cancellationToken = default);
}
