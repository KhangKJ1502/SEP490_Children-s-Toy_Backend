namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Request payload for checkout confirmation.
/// </summary>
public class CheckoutConfirmRequestDto
{
    public int AccountId { get; set; }

    public int AddressId { get; set; }

    public string PaymentMethod { get; set; } = "SHIP_COD";

    public decimal VoucherDiscountAmount { get; set; }

    public decimal CodValue { get; set; }

    public string? Note { get; set; }

    /// <summary>Mã voucher tổng đơn (tuỳ chọn).</summary>
    public string? OrderVoucherCode { get; set; }

    /// <summary>Mã voucher vận chuyển (tuỳ chọn).</summary>
    public string? ShippingVoucherCode { get; set; }

    /// <summary>Mã voucher (legacy - sẽ map vào OrderVoucherCode nếu có).</summary>
    public string? VoucherCode { get; set; }

    public List<CheckoutConfirmItemDto> Items { get; set; } = [];
}