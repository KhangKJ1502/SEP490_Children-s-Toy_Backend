namespace ToyStore.Application.Common.Helpers;

/// <summary>
/// String manipulation helper methods.
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// Generates a URL-friendly slug from a string.
    /// </summary>
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convert to lowercase
        text = text.ToLowerInvariant();
        
        // Replace Vietnamese characters
        text = RemoveVietnameseAccents(text);
        
        // Remove invalid characters
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9\s-]", "");
        
        // Replace multiple spaces with single space
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        
        // Replace spaces with hyphens
        text = text.Replace(" ", "-");
        
        // Remove multiple hyphens
        text = System.Text.RegularExpressions.Regex.Replace(text, @"-+", "-");
        
        return text;
    }
    
    /// <summary>
    /// Removes Vietnamese diacritics from a string.
    /// </summary>
    public static string RemoveVietnameseAccents(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var replacements = new Dictionary<string, string>
        {
            { "[àáạảãâầấậẩẫăằắặẳẵ]", "a" },
            { "[èéẹẻẽêềếệểễ]", "e" },
            { "[ìíịỉĩ]", "i" },
            { "[òóọỏõôồốộổỗơờớợởỡ]", "o" },
            { "[ùúụủũưừứựửữ]", "u" },
            { "[ỳýỵỷỹ]", "y" },
            { "[đ]", "d" }
        };

        foreach (var (pattern, replacement) in replacements)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, pattern, replacement);
        }

        return text;
    }
    
    /// <summary>
    /// Truncates a string to a specified length with ellipsis.
    /// </summary>
    public static string Truncate(string text, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
            
        return text[..(maxLength - suffix.Length)] + suffix;
    }
    
    /// <summary>
    /// Masks sensitive information like email or phone.
    /// </summary>
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return email;

        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];
        
        if (name.Length <= 2)
            return email;
            
        var maskedName = name[0] + new string('*', name.Length - 2) + name[^1];
        return $"{maskedName}@{domain}";
    }
    
    /// <summary>
    /// Masks phone number showing only last 4 digits.
    /// </summary>
    public static string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4)
            return phone;

        return new string('*', phone.Length - 4) + phone[^4..];
    }
}
