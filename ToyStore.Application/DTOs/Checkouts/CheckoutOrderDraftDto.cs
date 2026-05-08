namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Order draft for persistence.
/// </summary>
public class CheckoutOrderDraftDto
{
    public int AccountId { get; set; }

    public byte StatusId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public string ShippingName { get; set; } = string.Empty;

    public string ShippingPhone { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public string ShippingWardCode { get; set; } = string.Empty;

    public string ShippingWardName { get; set; } = string.Empty;

    public int ShippingDistrictId { get; set; }

    public string ShippingDistrictName { get; set; } = string.Empty;

    public int ShippingProvinceId { get; set; }

    public string ShippingProvinceName { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = "PENDING";

    public decimal SubTotal { get; set; }

    public decimal VoucherDiscountAmount { get; set; }

    public decimal EstimatedShippingFee { get; set; }

    public decimal TotalAmount { get; set; }
}