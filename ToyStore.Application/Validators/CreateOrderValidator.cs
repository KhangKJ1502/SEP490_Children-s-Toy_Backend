using ToyStore.Application.Common.Extensions;
using ToyStore.Application.DTOs;

namespace ToyStore.Application.Validators;

/// <summary>
/// Validator for CreateOrderDto.
/// </summary>
public static class CreateOrderValidator
{
    public static ValidationResult Validate(CreateOrderDto dto)
    {
        var errors = new Dictionary<string, List<string>>();
        
        // User validation
        if (dto.UserId == Guid.Empty)
        {
            AddError(errors, nameof(dto.UserId), "Người dùng không hợp lệ.");
        }
        
        // Recipient name validation
        if (string.IsNullOrWhiteSpace(dto.RecipientName))
        {
            AddError(errors, nameof(dto.RecipientName), "Tên người nhận không được để trống.");
        }
        else if (dto.RecipientName.Length < 2)
        {
            AddError(errors, nameof(dto.RecipientName), "Tên người nhận phải có ít nhất 2 ký tự.");
        }
        
        // Phone validation
        if (string.IsNullOrWhiteSpace(dto.RecipientPhone))
        {
            AddError(errors, nameof(dto.RecipientPhone), "Số điện thoại không được để trống.");
        }
        else if (!dto.RecipientPhone.IsValidVietnamesePhone())
        {
            AddError(errors, nameof(dto.RecipientPhone), "Số điện thoại không hợp lệ.");
        }
        
        // Address validation
        if (string.IsNullOrWhiteSpace(dto.ShippingAddress))
        {
            AddError(errors, nameof(dto.ShippingAddress), "Địa chỉ giao hàng không được để trống.");
        }
        else if (dto.ShippingAddress.Length < 10)
        {
            AddError(errors, nameof(dto.ShippingAddress), "Địa chỉ giao hàng quá ngắn.");
        }
        
        // Items validation
        if (dto.Items == null || dto.Items.Count == 0)
        {
            AddError(errors, nameof(dto.Items), "Đơn hàng phải có ít nhất một sản phẩm.");
        }
        else
        {
            for (int i = 0; i < dto.Items.Count; i++)
            {
                var item = dto.Items[i];
                
                if (item.ProductId == Guid.Empty)
                {
                    AddError(errors, $"Items[{i}].ProductId", "Sản phẩm không hợp lệ.");
                }
                
                if (item.Quantity <= 0)
                {
                    AddError(errors, $"Items[{i}].Quantity", "Số lượng phải lớn hơn 0.");
                }
                else if (item.Quantity > 100)
                {
                    AddError(errors, $"Items[{i}].Quantity", "Số lượng không được vượt quá 100.");
                }
            }
            
            // Check for duplicate products
            var duplicates = dto.Items
                .GroupBy(x => x.ProductId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
                
            if (duplicates.Any())
            {
                AddError(errors, nameof(dto.Items), "Có sản phẩm bị trùng lặp trong đơn hàng.");
            }
        }
        
        // Payment method validation
        var validPaymentMethods = new[] { "COD", "VNPay", "Momo", "ZaloPay", "BankTransfer" };
        if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
        {
            AddError(errors, nameof(dto.PaymentMethod), "Vui lòng chọn phương thức thanh toán.");
        }
        else if (!validPaymentMethods.Contains(dto.PaymentMethod))
        {
            AddError(errors, nameof(dto.PaymentMethod), "Phương thức thanh toán không hợp lệ.");
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
