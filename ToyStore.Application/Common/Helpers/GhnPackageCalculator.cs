using System;
using System.Collections.Generic;
using System.Linq;

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
                ServiceTypeId = 2,
                Length = 0,
                Width = 0,
                Height = 0,
                Weight = 0,
                InsuranceValue = 0,
                Items = new List<GhnItem>()
            };
        }

        // Chuẩn hóa và áp dụng giá trị mặc định nếu sản phẩm chưa được cấu hình kích thước/cân nặng
        var sanitizedItems = items.Select(i => new ShippingItem(
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

        // 1. Tính tổng actual weight
        int actualWeight = sanitizedItems.Sum(i => i.WeightGram * i.Quantity);

        // 2. Tính InsuranceValue = Sum(UnitPrice * Quantity) cast int
        int insuranceValue = (int)sanitizedItems.Sum(i => i.UnitPrice * i.Quantity);

        // 3. Tính kích thước quy đổi của hàng nhẹ (Type 2) trước để xác định trọng lượng tính cước thực tế (Billable Weight)
        int maxL = sanitizedItems.Max(i => i.LengthCm);
        int maxW = sanitizedItems.Max(i => i.WidthCm);
        int sumH = sanitizedItems.Sum(i => i.HeightCm * i.Quantity);
        int volumetric = (maxL * maxW * sumH) / 5;

        int billableWeight = Math.Max(actualWeight, volumetric);

        // 4. Phân nhánh dựa theo billableWeight (Dưới 20kg dùng Service 2, >=20kg dùng Service 5)
        if (billableWeight < 20000)
        {
            return ForType2(sanitizedItems, insuranceValue, maxL, maxW, sumH, billableWeight);
        }
        else
        {
            return ForType5(sanitizedItems, insuranceValue);
        }
    }

    private static GhnPackage ForType2(List<ShippingItem> items, int insuranceValue, int maxL, int maxW, int sumH, int billable)
    {
        var ghnItems = items.Select(i => new GhnItem
        {
            Name = i.ProductName,
            Code = i.ProductId.ToString(),
            Quantity = i.Quantity,
            Price = (int)i.UnitPrice,
            Length = i.LengthCm,
            Width = i.WidthCm,
            Height = i.HeightCm,
            Weight = i.WeightGram,
            Category = i.CategoryName
        }).ToList();

        return new GhnPackage
        {
            ServiceTypeId = 2,
            Length = Math.Min(maxL, 150),
            Width = Math.Min(maxW, 150),
            Height = Math.Min(sumH, 150),
            Weight = Math.Min(billable, 20000),
            InsuranceValue = insuranceValue,
            Items = ghnItems
        };
    }

    private static GhnPackage ForType5(List<ShippingItem> items, int insuranceValue)
    {
        var ghnItems = new List<GhnItem>();

        foreach (var item in items)
        {
            // Sắp xếp 3 chiều: Lớn nhất -> length, Trung bình -> width, Nhỏ nhất -> height
            var dims = new[] { item.LengthCm, item.WidthCm, item.HeightCm };
            Array.Sort(dims);
            Array.Reverse(dims);

            int l = dims[0]; // Lớn nhất
            int w = dims[1]; // Trung bình
            int h = dims[2]; // Nhỏ nhất

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
            ServiceTypeId = 5,
            Length = Math.Min(maxL, 200),
            Width = Math.Min(maxW, 200),
            Height = Math.Min(sumH, 200),
            Weight = Math.Min(sumW, 50000), // Cap at 50kg to prevent GHN HTTP 400
            InsuranceValue = insuranceValue,
            Items = ghnItems
        };
    }
}
