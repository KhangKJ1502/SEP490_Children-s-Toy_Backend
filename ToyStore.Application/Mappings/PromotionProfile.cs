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
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdatePromotionDto, Promotion>()
            .ForAllMembers(opt =>
                opt.Condition((_, _, srcMember) => srcMember is not null));

        CreateMap<ProductPromotion, ProductPromotionDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : string.Empty));

        CreateMap<CreateProductPromotionDto, ProductPromotion>()
            .ForMember(dest => dest.SoldQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.ReservedQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.PromotionId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.Promotion, opt => opt.Ignore());
    }
}
