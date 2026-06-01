using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToyStore.Application.DTOs.Checkouts;

namespace ToyStore.Application.Common.Helpers;

// Input — build từ query EF Core
public record ShippingItem(
    int     ProductId,
    string  ProductName,
    string  CategoryName,
    int     Quantity,
    decimal UnitPrice,
    int     WeightGram,   // ProductDetails.WeightGram  ← đọc trực tiếp từ DB
    int     LengthCm,     // ProductDetails.LengthCm
    int     WidthCm,      // ProductDetails.WidthCm
    int     HeightCm      // ProductDetails.HeightCm
);

// Output — map sang GHN API request
public class GhnPackage
{
    public int          ServiceTypeId  { get; set; }
    public int          Length         { get; set; }
    public int          Width          { get; set; }
    public int          Height         { get; set; }
    public int          Weight         { get; set; }  // billable
    public int          InsuranceValue { get; set; }
    public List<GhnItem> Items         { get; set; } = new();
}

public class GhnItem
{
    public string Name     { get; set; } = "";
    public string Code     { get; set; } = "";
    public int    Quantity { get; set; }
    public int    Price    { get; set; }
    public int    Length   { get; set; }
    public int    Width    { get; set; }
    public int    Height   { get; set; }
    public int    Weight   { get; set; }  // billable per item
    public string Category { get; set; } = "";
}

public record GhnDimensionViolation(
    string ProductName,
    int LengthCm,
    int WidthCm,
    int HeightCm,
    int MaxAllowedCm);

public static class GhnShippingLimits
{
    public const int Type2MaxCm = 150;
    public const int Type5MaxCm = 200;
    public const int Type2ServiceId = 2;
    public const int Type5ServiceId = 5;

    public static int MaxDimensionCm(int serviceTypeId) =>
        serviceTypeId == Type5ServiceId ? Type5MaxCm : Type2MaxCm;

    public static bool HasUnshippableDimensions(IEnumerable<ShippingItem> items) =>
        items.Any(i => i.LengthCm > Type5MaxCm || i.WidthCm > Type5MaxCm || i.HeightCm > Type5MaxCm);

    public static List<GhnDimensionViolation> GetViolations(
        IEnumerable<ShippingItem> items,
        int serviceTypeId)
    {
        var maxAllowed = MaxDimensionCm(serviceTypeId);
        var violations = new List<GhnDimensionViolation>();

        foreach (var item in items)
        {
            if (item.LengthCm > maxAllowed || item.WidthCm > maxAllowed || item.HeightCm > maxAllowed)
            {
                violations.Add(new GhnDimensionViolation(
                    item.ProductName,
                    item.LengthCm,
                    item.WidthCm,
                    item.HeightCm,
                    maxAllowed));
            }
        }

        return violations;
    }

    public static string FormatViolationMessage(IReadOnlyList<GhnDimensionViolation> violations)
    {
        if (violations.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append("Product dimensions exceed GHN shipping limits. ");

        foreach (var v in violations)
        {
            sb.Append($"Product '{v.ProductName}' ({v.LengthCm}×{v.WidthCm}×{v.HeightCm} cm) exceeds the {v.MaxAllowedCm} cm limit. ");
        }

        sb.Append("Please review and update the product dimensions.");
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Final guard before calling GHN create-order API.
    /// Upgrades service type when needed and clamps package/item dimensions to GHN limits.
    /// </summary>
    public static bool TryNormalizeCreateOrderRequest(ShippingOrderCreateRequestDto request, out string? errorMessage)
    {
        errorMessage = null;

        if (request.ServiceTypeId <= 0)
            request.ServiceTypeId = Type2ServiceId;

        var maxRawSide = MaxSide(request.Length, request.Width, request.Height);
        foreach (var item in request.Items)
            maxRawSide = Math.Max(maxRawSide, MaxSide(item.Length, item.Width, item.Height));

        if (maxRawSide > Type5MaxCm)
        {
            errorMessage =
                $"Product dimensions exceed the maximum GHN limit of {Type5MaxCm} cm. " +
                $"Largest side detected: {maxRawSide} cm.";
            return false;
        }

        if (request.ServiceTypeId == Type2ServiceId &&
            (maxRawSide > Type2MaxCm || request.Weight >= 20000))
        {
            request.ServiceTypeId = Type5ServiceId;
        }

        var maxCm = MaxDimensionCm(request.ServiceTypeId);
        (request.Length, request.Width, request.Height) =
            GhnPackageCalculator.NormalizeAndClampDimensions(request.Length, request.Width, request.Height, maxCm);

        foreach (var item in request.Items)
        {
            if (item.Length <= 0 && item.Width <= 0 && item.Height <= 0)
                continue;

            var length = item.Length > 0 ? item.Length : 1;
            var width = item.Width > 0 ? item.Width : 1;
            var height = item.Height > 0 ? item.Height : 1;
            (item.Length, item.Width, item.Height) =
                GhnPackageCalculator.NormalizeAndClampDimensions(length, width, height, maxCm);
        }

        return true;
    }

    private static int MaxSide(int length, int width, int height) =>
        Math.Max(length, Math.Max(width, height));
}

public static class GhnPackageCalculator
{
    public static GhnPackage Calculate(
        List<ShippingItem> items,
        int defaultWeight = 100,
        int defaultLength = 15,
        int defaultWidth = 15,
        int defaultHeight = 5)
    {
        if (items == null || !items.Any())
        {
            return new GhnPackage
            {
                ServiceTypeId = GhnShippingLimits.Type2ServiceId,
                Length = 0,
                Width = 0,
                Height = 0,
                Weight = 0,
                InsuranceValue = 0,
                Items = new List<GhnItem>()
            };
        }

        var sanitizedItems = SanitizeItems(items, defaultWeight, defaultLength, defaultWidth, defaultHeight);

        int actualWeight = sanitizedItems.Sum(i => i.WeightGram * i.Quantity);
        int insuranceValue = (int)sanitizedItems.Sum(i => i.UnitPrice * i.Quantity);

        int maxL = sanitizedItems.Max(i => i.LengthCm);
        int maxW = sanitizedItems.Max(i => i.WidthCm);
        int sumH = sanitizedItems.Sum(i => i.HeightCm * i.Quantity);
        int volumetric = (maxL * maxW * sumH) / 5;

        int billableWeight = Math.Max(actualWeight, volumetric);
        int maxSingleSide = sanitizedItems.Max(i => Math.Max(i.LengthCm, Math.Max(i.WidthCm, i.HeightCm)));

        if (billableWeight >= 20000 || maxSingleSide > GhnShippingLimits.Type2MaxCm)
            return ForType5(sanitizedItems, insuranceValue);

        return ForType2(sanitizedItems, insuranceValue, maxL, maxW, sumH, billableWeight);
    }

    /// <summary>
    /// Builds a GHN package for the resolved service type.
    /// Heavy or bulky orders are always upgraded to service type 5.
    /// </summary>
    public static GhnPackage CalculateForServiceType(
        List<ShippingItem> items,
        int requestedServiceTypeId,
        int defaultWeight = 100,
        int defaultLength = 15,
        int defaultWidth = 15,
        int defaultHeight = 5)
    {
        if (items == null || !items.Any())
        {
            return new GhnPackage
            {
                ServiceTypeId = GhnShippingLimits.Type2ServiceId,
                Length = 0,
                Width = 0,
                Height = 0,
                Weight = 0,
                InsuranceValue = 0,
                Items = new List<GhnItem>()
            };
        }

        var sanitizedItems = SanitizeItems(items, defaultWeight, defaultLength, defaultWidth, defaultHeight);

        int actualWeight = sanitizedItems.Sum(i => i.WeightGram * i.Quantity);
        int insuranceValue = (int)sanitizedItems.Sum(i => i.UnitPrice * i.Quantity);

        int maxL = sanitizedItems.Max(i => i.LengthCm);
        int maxW = sanitizedItems.Max(i => i.WidthCm);
        int sumH = sanitizedItems.Sum(i => i.HeightCm * i.Quantity);
        int volumetric = (maxL * maxW * sumH) / 5;
        int billableWeight = Math.Max(actualWeight, volumetric);
        int maxSingleSide = sanitizedItems.Max(i => Math.Max(i.LengthCm, Math.Max(i.WidthCm, i.HeightCm)));

        var serviceTypeId = requestedServiceTypeId > 0
            ? requestedServiceTypeId
            : GhnShippingLimits.Type2ServiceId;

        if (billableWeight >= 20000 || maxSingleSide > GhnShippingLimits.Type2MaxCm)
            serviceTypeId = GhnShippingLimits.Type5ServiceId;

        if (serviceTypeId == GhnShippingLimits.Type5ServiceId)
            return ForType5(sanitizedItems, insuranceValue);

        return ForType2(sanitizedItems, insuranceValue, maxL, maxW, sumH, billableWeight);
    }

    public static (int Length, int Width, int Height) NormalizeAndClampDimensions(
        int length,
        int width,
        int height,
        int maxCm) => ClampDimensions(length, width, height, maxCm);

    public static List<ShippingItem> SanitizeItems(
        List<ShippingItem> items,
        int defaultWeight,
        int defaultLength,
        int defaultWidth,
        int defaultHeight)
    {
        return items.Select(i => new ShippingItem(
            i.ProductId,
            i.ProductName,
            i.CategoryName,
            i.Quantity,
            i.UnitPrice,
            i.WeightGram > 0 ? i.WeightGram : defaultWeight,
            i.LengthCm > 0 ? i.LengthCm : defaultLength,
            i.WidthCm > 0 ? i.WidthCm : defaultWidth,
            i.HeightCm > 0 ? i.HeightCm : defaultHeight
        )).ToList();
    }

    private static (int Length, int Width, int Height) ClampDimensions(int length, int width, int height, int maxCm)
    {
        var dims = new[] { length, width, height };
        Array.Sort(dims);
        Array.Reverse(dims);

        return (
            Math.Min(dims[0], maxCm),
            Math.Min(dims[1], maxCm),
            Math.Min(dims[2], maxCm));
    }

    private static GhnPackage ForType2(
        List<ShippingItem> items,
        int insuranceValue,
        int maxL,
        int maxW,
        int sumH,
        int billable)
    {
        const int maxCm = GhnShippingLimits.Type2MaxCm;

        var ghnItems = items.Select(i =>
        {
            var (l, w, h) = ClampDimensions(i.LengthCm, i.WidthCm, i.HeightCm, maxCm);
            return new GhnItem
            {
                Name = i.ProductName,
                Code = i.ProductId.ToString(),
                Quantity = i.Quantity,
                Price = (int)i.UnitPrice,
                Length = l,
                Width = w,
                Height = h,
                Weight = i.WeightGram,
                Category = i.CategoryName
            };
        }).ToList();

        return new GhnPackage
        {
            ServiceTypeId = GhnShippingLimits.Type2ServiceId,
            Length = Math.Min(maxL, maxCm),
            Width = Math.Min(maxW, maxCm),
            Height = Math.Min(sumH, maxCm),
            Weight = Math.Min(billable, 20000),
            InsuranceValue = insuranceValue,
            Items = ghnItems
        };
    }

    private static GhnPackage ForType5(List<ShippingItem> items, int insuranceValue)
    {
        const int maxCm = GhnShippingLimits.Type5MaxCm;
        var ghnItems = new List<GhnItem>();

        foreach (var item in items)
        {
            var (l, w, h) = ClampDimensions(item.LengthCm, item.WidthCm, item.HeightCm, maxCm);

            int volumetricItem = (l * w * h) / 5;
            int billableItem = Math.Max(item.WeightGram, volumetricItem);

            ghnItems.Add(new GhnItem
            {
                Name = item.ProductName,
                Code = item.ProductId.ToString(),
                Quantity = item.Quantity,
                Price = (int)item.UnitPrice,
                Length = l,
                Width = w,
                Height = h,
                Weight = billableItem,
                Category = item.CategoryName
            });
        }

        int maxL = ghnItems.Max(i => i.Length);
        int maxW = ghnItems.Max(i => i.Width);
        int sumH = ghnItems.Sum(i => i.Height * i.Quantity);
        int sumW = ghnItems.Sum(i => i.Weight * i.Quantity);

        return new GhnPackage
        {
            ServiceTypeId = GhnShippingLimits.Type5ServiceId,
            Length = Math.Min(maxL, maxCm),
            Width = Math.Min(maxW, maxCm),
            Height = Math.Min(sumH, maxCm),
            Weight = Math.Min(sumW, 50000),
            InsuranceValue = insuranceValue,
            Items = ghnItems
        };
    }
}
