namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Active address info used for checkout.
/// </summary>
public class CheckoutAddressInfoDto
{
    public int AddressId { get; set; }

    public int AccountId { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string AddressLine { get; set; } = string.Empty;

    public int ProvinceId { get; set; }

    public int DistrictId { get; set; }

    public string WardCode { get; set; } = string.Empty;

    public string ProvinceName { get; set; } = string.Empty;

    public string DistrictName { get; set; } = string.Empty;

    public string WardName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}