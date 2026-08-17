namespace ToyStore.Application.DTOs.Refunds;

/// <summary>
/// Data Transfer Object (DTO) chứa thông tin sản phẩm và số lượng sản phẩm trong yêu cầu hoàn tiền.
/// </summary>
public class CreateRefundItemDto
{
    /// <summary>
    /// Mã ID của sản phẩm cần hoàn trả.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Số lượng sản phẩm yêu cầu hoàn trả (phải lớn hơn 0 và không vượt quá số lượng đã mua trong đơn hàng gốc).
    /// </summary>
    public short Quantity { get; set; }
}
