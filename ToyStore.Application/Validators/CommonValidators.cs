using ToyStore.Application.Common.Extensions;

namespace ToyStore.Application.Validators;

/// <summary>
/// Common validation methods.
/// </summary>
public static class CommonValidators
{
    /// <summary>
    /// Validates email format.
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
            
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Validates Vietnamese phone number.
    /// </summary>
    public static bool IsValidPhone(string? phone)
    {
        return !string.IsNullOrEmpty(phone) && phone.IsValidVietnamesePhone();
    }
    
    /// <summary>
    /// Validates password strength.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "Mật khẩu không được để trống.");
            
        if (password.Length < 8)
            return (false, "Mật khẩu phải có ít nhất 8 ký tự.");
            
        if (!password.Any(char.IsUpper))
            return (false, "Mật khẩu phải có ít nhất một chữ hoa.");
            
        if (!password.Any(char.IsLower))
            return (false, "Mật khẩu phải có ít nhất một chữ thường.");
            
        if (!password.Any(char.IsDigit))
            return (false, "Mật khẩu phải có ít nhất một chữ số.");
            
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return (false, "Mật khẩu phải có ít nhất một ký tự đặc biệt.");
            
        return (true, null);
    }
    
    /// <summary>
    /// Validates GUID format.
    /// </summary>
    public static bool IsValidGuid(string? value)
    {
        return Guid.TryParse(value, out _);
    }
    
    /// <summary>
    /// Validates URL format.
    /// </summary>
    public static bool IsValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) 
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
    
    /// <summary>
    /// Validates date is in the future.
    /// </summary>
    public static bool IsFutureDate(DateTime date)
    {
        return date > DateTime.UtcNow;
    }
    
    /// <summary>
    /// Validates date is in the past.
    /// </summary>
    public static bool IsPastDate(DateTime date)
    {
        return date < DateTime.UtcNow;
    }
    
    /// <summary>
    /// Validates Vietnamese tax code.
    /// </summary>
    public static bool IsValidTaxCode(string? taxCode)
    {
        if (string.IsNullOrEmpty(taxCode))
            return false;
            
        // Vietnamese tax code: 10 or 14 digits
        return System.Text.RegularExpressions.Regex.IsMatch(
            taxCode, @"^\d{10}(-\d{3})?$");
    }
}
