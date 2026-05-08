namespace ToyStore.Infrastructure.Options;

/// <summary>
/// Cau hinh dia chi gui hang cho GHN.
/// </summary>
public class ShopAddressOptions
{
    public const string SectionName = "ShopAddress";

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string AddressLine { get; set; } = string.Empty;

    public string WardCode { get; set; } = string.Empty;

    public string WardName { get; set; } = string.Empty;

    public int DistrictId { get; set; }

    public string DistrictName { get; set; } = string.Empty;

    public string ProvinceName { get; set; } = string.Empty;
}