using AutoMapper;
using ToyStore.Application.DTOs.Promotions;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper cho promotion.
/// </summary>
public class PromotionProfile : Profile
{
    public PromotionProfile()
    {
        CreateMap<Promotion, PromotionDto>();

        CreateMap<Promotion, PromotionListDto>();

        CreateMap<Promotion, CreatePromotionDto>();

        CreateMap<CreatePromotionDto, Promotion>()
            .ForMember(dest => dest.PromotionId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ProductPromotions, opt => opt.Ignore())
            .ForMember(dest => dest.PromotionTimeSlots, opt => opt.Ignore());

        CreateMap<UpdatePromotionDto, Promotion>()
            .ForMember(dest => dest.ProductPromotions, opt => opt.Ignore())
            .ForMember(dest => dest.PromotionTimeSlots, opt => opt.Ignore())
            .ForAllMembers(opt =>
                opt.Condition((_, _, srcMember) => srcMember is not null));

        CreateMap<ProductPromotion, ProductPromotionDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : string.Empty))
            .ForMember(dest => dest.OriginalPrice, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price : 0))
            .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.Product != null ? src.Product.Quantity : 0));

        CreateMap<CreateProductPromotionDto, ProductPromotion>()
            .ForMember(dest => dest.SoldQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.ReservedQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.PromotionId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.Promotion, opt => opt.Ignore());

        CreateMap<ProductPromotion, CreateProductPromotionDto>();

        CreateMap<PromotionTimeSlot, PromotionTimeSlotDto>();
        CreateMap<PromotionTimeSlot, CreatePromotionTimeSlotDto>();

        CreateMap<CreatePromotionTimeSlotDto, PromotionTimeSlot>()
            .ForMember(dest => dest.TimeSlotId, opt => opt.Ignore())
            .ForMember(dest => dest.PromotionId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Promotion, opt => opt.Ignore());

        CreateMap<PromotionProductSlot, PromotionProductSlotDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : string.Empty))
            .ForMember(dest => dest.OriginalPrice, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price : 0))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.Product != null && src.Product.ProductImage != null ? src.Product.ProductImage.ImageUrl : null));

        CreateMap<CreatePromotionProductSlotDto, PromotionProductSlot>()
            .ForMember(dest => dest.SlotProductId, opt => opt.Ignore())
            .ForMember(dest => dest.TimeSlotId, opt => opt.Ignore())
            .ForMember(dest => dest.SoldQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.ReservedQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.TimeSlot, opt => opt.Ignore());

        CreateMap<PromotionProductSlot, CreatePromotionProductSlotDto>();
    }
}
