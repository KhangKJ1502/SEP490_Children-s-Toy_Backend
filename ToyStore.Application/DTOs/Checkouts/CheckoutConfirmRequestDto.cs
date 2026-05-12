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

    /// <summary>Mã voucher (tuỳ chọn).</summary>
    public string? VoucherCode { get; set; }

    public List<CheckoutConfirmItemDto> Items { get; set; } = [];
}