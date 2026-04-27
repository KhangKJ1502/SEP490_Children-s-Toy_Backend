namespace ToyStore.Application.DTOs.Brands;

public class BrandListDto
{
    public short BrandId { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
