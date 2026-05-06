namespace ToyStore.Application.DTOs.Addresses;

public class ProvinceOptionDto
{
    public int ProvinceId { get; set; }
    public string ProvinceName { get; set; } = string.Empty;
}

public class DistrictOptionDto
{
    public int DistrictId { get; set; }
    public int ProvinceId { get; set; }
    public string DistrictName { get; set; } = string.Empty;
}

public class WardOptionDto
{
    public string WardCode { get; set; } = string.Empty;
    public int DistrictId { get; set; }
    public string WardName { get; set; } = string.Empty;
}
