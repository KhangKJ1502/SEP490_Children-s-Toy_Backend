namespace ToyStore.Application.Common.Helpers;

/// <summary>
/// Money formatting helper for Vietnamese Dong (VND).
/// </summary>
public static class MoneyHelper
{
    /// <summary>
    /// Formats amount as Vietnamese currency.
    /// </summary>
    public static string FormatVND(decimal amount)
    {
        return amount % 1 == 0 ? $"{amount:N0} VND" : $"{amount:#,##0.######} VND";
    }
    
    /// <summary>
    /// Formats amount with thousands separator.
    /// </summary>
    public static string FormatWithSeparator(decimal amount)
    {
        return amount % 1 == 0 ? amount.ToString("N0") : amount.ToString("#,##0.######");
    }
    
    /// <summary>
    /// Converts VND to readable format (e.g., 1.5 triệu).
    /// </summary>
    public static string ToReadableVND(decimal amount)
    {
        if (amount >= 1_000_000_000)
            return $"{amount / 1_000_000_000:0.#}B";
        if (amount >= 1_000_000)
            return $"{amount / 1_000_000:0.#}M";
        if (amount >= 1_000)
            return $"{amount / 1_000:0.#}K";
        
        return FormatVND(amount);
    }
    
    /// <summary>
    /// Calculates discount percentage.
    /// </summary>
    public static int CalculateDiscountPercentage(decimal originalPrice, decimal salePrice)
    {
        if (originalPrice <= 0 || salePrice >= originalPrice)
            return 0;
            
        return (int)Math.Round((1 - salePrice / originalPrice) * 100);
    }
    
    /// <summary>
    /// Rounds to nearest VND denomination.
    /// </summary>
    public static decimal RoundToNearest(decimal amount, decimal denomination = 1000)
    {
        return Math.Round(amount / denomination) * denomination;
    }
}
