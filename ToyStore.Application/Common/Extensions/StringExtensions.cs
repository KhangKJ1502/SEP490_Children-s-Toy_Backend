namespace ToyStore.Application.Common.Extensions;

/// <summary>
/// String extension methods.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if string is null or whitespace.
    /// </summary>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
    
    /// <summary>
    /// Returns null if string is empty or whitespace.
    /// </summary>
    public static string? NullIfEmpty(this string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
    
    /// <summary>
    /// Converts first character to uppercase.
    /// </summary>
    public static string ToTitleCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
            
        return char.ToUpper(value[0]) + value[1..].ToLower();
    }
    
    /// <summary>
    /// Converts to snake_case.
    /// </summary>
    public static string ToSnakeCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
            
        return string.Concat(
            value.Select((c, i) => 
                i > 0 && char.IsUpper(c) ? "_" + c.ToString().ToLower() : c.ToString().ToLower()));
    }
    
    /// <summary>
    /// Converts to kebab-case.
    /// </summary>
    public static string ToKebabCase(this string value)
    {
        return value.ToSnakeCase().Replace('_', '-');
    }
    
    /// <summary>
    /// Removes all whitespace from string.
    /// </summary>
    public static string RemoveWhitespace(this string value)
    {
        return string.Concat(value.Where(c => !char.IsWhiteSpace(c)));
    }
    
    /// <summary>
    /// Checks if string contains only digits.
    /// </summary>
    public static bool IsNumeric(this string value)
    {
        return !string.IsNullOrEmpty(value) && value.All(char.IsDigit);
    }
    
    /// <summary>
    /// Validates Vietnamese phone number format.
    /// </summary>
    public static bool IsValidVietnamesePhone(this string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return false;
            
        var cleaned = phone.RemoveWhitespace().Replace("-", "");
        return System.Text.RegularExpressions.Regex.IsMatch(
            cleaned, @"^(0|84|\+84)(3|5|7|8|9)\d{8}$");
    }
}
