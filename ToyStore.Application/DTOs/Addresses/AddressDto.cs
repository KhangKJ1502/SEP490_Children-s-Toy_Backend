namespace ToyStore.Application.DTOs.Addresses;

public class AddressDto
{
    public int AddressId { get; set; }
    public string? RecipientName { get; set; }
    public string? PhoneNumber { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? WardCode { get; set; }
    public string? WardName { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public int? ProvinceId { get; set; }
    public string? ProvinceName { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}
