namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) thể hiện thông tin một sản phẩm từ đơn hàng đã hoàn tất mà khách hàng chưa thực hiện đánh giá.
/// </summary>
public class UnreviewedProductDto
{
    /// <summary>
    /// Mã ID sản phẩm.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Tên sản phẩm.
    /// </summary>
    public string ProductName { get; set; } = null!;

    /// <summary>
    /// URL ảnh đại diện của sản phẩm.
    /// </summary>
    public string? ProductImage { get; set; }

    /// <summary>
    /// Mã ID đơn hàng hoàn tất chứa sản phẩm này.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Mã code hiển thị của đơn hàng.
    /// </summary>
    public string OrderCode { get; set; } = null!;

    /// <summary>
    /// Thời điểm đơn hàng hoàn tất (CompletedAt).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Số ngày còn lại để khách hàng có thể gửi đánh giá (tối đa 20 ngày kể từ khi đơn hàng hoàn tất).
    /// </summary>
    public int RemainingDays { get; set; }
}
