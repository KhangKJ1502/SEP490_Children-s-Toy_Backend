namespace ToyStore.Application.DTOs.Products;

public class ProductLookupsDto
{
    public List<SuperCategoryLookupDto> SuperCategories { get; set; } = [];

    public List<CategoryLookupDto> Categories { get; set; } = [];

    public List<BrandLookupDto> Brands { get; set; } = [];

    public List<PriceRangeLookupDto> PriceRanges { get; set; } = [];

    public List<MaterialLookupDto> Materials { get; set; } = [];

    public List<AgeLookupDto> Ages { get; set; } = [];

    public List<SexLookupDto> Sexes { get; set; } = [];

    public List<OriginLookupDto> Origins { get; set; } = [];
}

public class SuperCategoryLookupDto
{
    public short Id { get; set; }

    public string Label { get; set; } = string.Empty;
}

public class CategoryLookupDto
{
    public short Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public short SuperCategoryId { get; set; }

    public string SuperCategoryName { get; set; } = string.Empty;
}

public class BrandLookupDto
{
    public short Id { get; set; }

    public string Label { get; set; } = string.Empty;
}

public class PriceRangeLookupDto
{
    public byte Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public decimal Min { get; set; }

    public decimal Max { get; set; }
}

public class MaterialLookupDto
{
    public short Id { get; set; }

    public string Label { get; set; } = string.Empty;
}

public class AgeLookupDto
{
    public byte Id { get; set; }

    public string Label { get; set; } = string.Empty;
}

public class SexLookupDto
{
    public byte Id { get; set; }

    public string Label { get; set; } = string.Empty;
}

public class OriginLookupDto
{
    public byte Id { get; set; }

    public string Label { get; set; } = string.Empty;
}
