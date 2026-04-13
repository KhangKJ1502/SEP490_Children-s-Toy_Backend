using ToyStore.Application.DTOs;

namespace ToyStore.Application.Validators;

/// <summary>
/// Validator for CreateProductDto.
/// </summary>
public static class CreateProductValidator
{
    public static ValidationResult Validate(CreateProductDto dto)
    {
        var errors = new Dictionary<string, List<string>>();
        
        // Name validation
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            AddError(errors, nameof(dto.Name), "Tên sản phẩm không được để trống.");
        }
        else if (dto.Name.Length < 3)
        {
            AddError(errors, nameof(dto.Name), "Tên sản phẩm phải có ít nhất 3 ký tự.");
        }
        else if (dto.Name.Length > 200)
        {
            AddError(errors, nameof(dto.Name), "Tên sản phẩm không được vượt quá 200 ký tự.");
        }
        
        // Price validation
        if (dto.Price <= 0)
        {
            AddError(errors, nameof(dto.Price), "Giá sản phẩm phải lớn hơn 0.");
        }
        else if (dto.Price > 100_000_000) // 100 million VND
        {
            AddError(errors, nameof(dto.Price), "Giá sản phẩm không được vượt quá 100.000.000 VND.");
        }
        
        // Sale price validation
        if (dto.SalePrice.HasValue)
        {
            if (dto.SalePrice.Value <= 0)
            {
                AddError(errors, nameof(dto.SalePrice), "Giá khuyến mãi phải lớn hơn 0.");
            }
            else if (dto.SalePrice.Value >= dto.Price)
            {
                AddError(errors, nameof(dto.SalePrice), "Giá khuyến mãi phải nhỏ hơn giá gốc.");
            }
        }
        
        // Stock validation
        if (dto.StockQuantity < 0)
        {
            AddError(errors, nameof(dto.StockQuantity), "Số lượng tồn kho không được âm.");
        }
        
        // SKU validation
        if (string.IsNullOrWhiteSpace(dto.SKU))
        {
            AddError(errors, nameof(dto.SKU), "Mã SKU không được để trống.");
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(dto.SKU, @"^[A-Z0-9\-]+$"))
        {
            AddError(errors, nameof(dto.SKU), "Mã SKU chỉ được chứa chữ hoa, số và dấu gạch ngang.");
        }
        
        // Category validation
        if (dto.CategoryId == Guid.Empty)
        {
            AddError(errors, nameof(dto.CategoryId), "Vui lòng chọn danh mục sản phẩm.");
        }
        
        return new ValidationResult(errors);
    }
    
    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();
        errors[field].Add(message);
    }
}

/// <summary>
/// Validation result containing errors.
/// </summary>
public class ValidationResult
{
    public Dictionary<string, List<string>> Errors { get; }
    public bool IsValid => Errors.Count == 0;
    
    public ValidationResult(Dictionary<string, List<string>> errors)
    {
        Errors = errors;
    }
    
    public Dictionary<string, string[]> ToErrorDictionary()
    {
        return Errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());
    }
}
