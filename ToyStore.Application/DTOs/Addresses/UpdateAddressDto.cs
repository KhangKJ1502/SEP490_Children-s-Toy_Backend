namespace ToyStore.Application.DTOs.Addresses;

public class UpdateAddressDto
{
    public string? RecipientName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AddressLine { get; set; }
    public string? WardCode { get; set; }
    public int? DistrictId { get; set; }
    public int? ProvinceId { get; set; }
    public bool? IsDefault { get; set; }
}
