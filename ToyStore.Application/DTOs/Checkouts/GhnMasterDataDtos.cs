namespace ToyStore.Application.DTOs.Checkouts;

public sealed class GhnProvinceDto
{
    public int ProvinceId { get; set; }
    public string ProvinceName { get; set; } = string.Empty;
    public string? ProvinceCode { get; set; }
}

public sealed class GhnDistrictDto
{
    public int DistrictId { get; set; }
    public int ProvinceId { get; set; }
    public string DistrictName { get; set; } = string.Empty;
}

public sealed class GhnWardDto
{
    public string WardCode { get; set; } = string.Empty;
    public int DistrictId { get; set; }
    public string WardName { get; set; } = string.Empty;
}
