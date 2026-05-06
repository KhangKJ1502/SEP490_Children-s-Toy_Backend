namespace ToyStore.Application.DTOs.Addresses;

public class CreateAddressDto
{
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string WardCode { get; set; } = string.Empty;
    public int DistrictId { get; set; }
    public int ProvinceId { get; set; }
    public bool IsDefault { get; set; }
}
