using AutoMapper;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper cho promotion.
/// </summary>
public class PromotionProfile : Profile
{
    public PromotionProfile()
    {
        CreateMap<PromotionModel, PromotionDto>();

        CreateMap<PromotionModel, PromotionListDto>();

        CreateMap<PromotionModel, CreatePromotionDto>();

        CreateMap<CreatePromotionDto, PromotionModel>()
            .ForMember(dest => dest.PromotionId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdatePromotionDto, PromotionModel>()
            .ForAllMembers(opt =>
                opt.Condition((_, _, srcMember) => srcMember is not null));
    }
}
