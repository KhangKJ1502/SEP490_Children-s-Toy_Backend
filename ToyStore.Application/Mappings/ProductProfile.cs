using AutoMapper;
using ToyStore.Application.DTOs.Products;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper cho Product.
/// </summary>
public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductListDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.BrandName : null))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.ProductImage != null ? src.ProductImage.ImageUrl : null));

        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.BrandName : null))
            .ForMember(dest => dest.PriceRangeMin, opt => opt.MapFrom(src => src.PriceRange != null ? src.PriceRange.PriceRangeMin : null))
            .ForMember(dest => dest.PriceRangeMax, opt => opt.MapFrom(src => src.PriceRange != null ? src.PriceRange.PriceRangeMax : null))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.Description : null))
            .ForMember(dest => dest.MaterialId, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.MaterialId : null))
            .ForMember(dest => dest.MaterialName, opt => opt.MapFrom(src => src.ProductDetail != null && src.ProductDetail.Material != null ? src.ProductDetail.Material.MaterialName : null))
            .ForMember(dest => dest.AgeId, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.AgeId : null))
            .ForMember(dest => dest.AgeRange, opt => opt.MapFrom(src => src.ProductDetail != null && src.ProductDetail.Age != null ? src.ProductDetail.Age.AgeRange : null))
            .ForMember(dest => dest.SexId, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.SexId : null))
            .ForMember(dest => dest.SexName, opt => opt.MapFrom(src => src.ProductDetail != null && src.ProductDetail.Sex != null ? src.ProductDetail.Sex.SexName : null))
            .ForMember(dest => dest.OriginId, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.OriginId : null))
            .ForMember(dest => dest.OriginName, opt => opt.MapFrom(src => src.ProductDetail != null && src.ProductDetail.Origin != null ? src.ProductDetail.Origin.OriginName : null))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.ProductImage != null ? src.ProductImage.ImageUrl : null));

        CreateMap<CreateProductDto, Product>()
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.LastLowStockNotifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Brand, opt => opt.Ignore())
            .ForMember(dest => dest.CartItems, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.ItemSimilaritySimilarProducts, opt => opt.Ignore())
            .ForMember(dest => dest.ItemSimilaritySourceProducts, opt => opt.Ignore())
            .ForMember(dest => dest.OrderDetails, opt => opt.Ignore())
            .ForMember(dest => dest.PriceRange, opt => opt.Ignore())
            .ForMember(dest => dest.ProductFollowers, opt => opt.Ignore())
            .ForMember(dest => dest.ProductPromotions, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewProducts, opt => opt.Ignore())
            .ForMember(dest => dest.TrendingProducts, opt => opt.Ignore())
            .ForMember(dest => dest.UserProductScores, opt => opt.Ignore())
            .ForMember(dest => dest.Wishlists, opt => opt.Ignore())
            .ForMember(dest => dest.ProductDetail, opt => opt.MapFrom(src => 
                !string.IsNullOrWhiteSpace(src.Description) || src.MaterialId.HasValue || src.AgeId.HasValue || src.SexId.HasValue || src.OriginId.HasValue
                ? new ProductDetail
                {
                    Description = src.Description,
                    MaterialId = src.MaterialId,
                    AgeId = src.AgeId,
                    SexId = src.SexId,
                    OriginId = src.OriginId
                }
                : null))
            .ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src => 
                !string.IsNullOrWhiteSpace(src.MainImageUrl)
                ? new ProductImage
                {
                    ImageUrl = src.MainImageUrl
                }
                : null));

        CreateMap<UpdateProductDto, Product>()
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.LastLowStockNotifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Brand, opt => opt.Ignore())
            .ForMember(dest => dest.CartItems, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.ItemSimilaritySimilarProducts, opt => opt.Ignore())
            .ForMember(dest => dest.ItemSimilaritySourceProducts, opt => opt.Ignore())
            .ForMember(dest => dest.OrderDetails, opt => opt.Ignore())
            .ForMember(dest => dest.PriceRange, opt => opt.Ignore())
            .ForMember(dest => dest.ProductFollowers, opt => opt.Ignore())
            .ForMember(dest => dest.ProductPromotions, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewProducts, opt => opt.Ignore())
            .ForMember(dest => dest.TrendingProducts, opt => opt.Ignore())
            .ForMember(dest => dest.UserProductScores, opt => opt.Ignore())
            .ForMember(dest => dest.Wishlists, opt => opt.Ignore())
            .ForMember(dest => dest.ProductDetail, opt => opt.Ignore())
            .ForMember(dest => dest.ProductImage, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
