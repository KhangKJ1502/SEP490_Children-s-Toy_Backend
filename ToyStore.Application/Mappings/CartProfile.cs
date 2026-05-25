using AutoMapper;
using ToyStore.Application.DTOs.Carts;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class CartProfile : Profile
{
    public CartProfile()
    {
        CreateMap<CartItem, CartItemDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))
            .ForMember(dest => dest.ProductStatus, opt => opt.MapFrom(src => src.Product.ProductStatus))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.Product.ProductImage != null ? src.Product.ProductImage.ImageUrl : null))
            .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.Product.Quantity))
            .ForMember(dest => dest.LineTotal, opt => opt.MapFrom(src => src.CurrentPrice * src.Quantity))
            .ForMember(dest => dest.IsSelected, opt => opt.MapFrom(_ => true));

        CreateMap<Cart, CartDto>()
            .ForMember(dest => dest.TotalItem, opt => opt.Ignore())
            .ForMember(dest => dest.TotalQuantity, opt => opt.Ignore())
            .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.CartItems.Where(x => x.RemovedAt == null)));
    }
}
