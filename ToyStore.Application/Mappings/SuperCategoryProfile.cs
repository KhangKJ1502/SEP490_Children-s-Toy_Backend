using AutoMapper;
using ToyStore.Application.DTOs.SuperCategories;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper cho SuperCategory.
/// </summary>
public class SuperCategoryProfile : Profile
{
    public SuperCategoryProfile()
    {
        CreateMap<SuperCategory, SuperCategoryListDto>();

        CreateMap<CreateSuperCategoryDto, SuperCategory>()
            .ForMember(dest => dest.SuperCategoryId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Categories, opt => opt.Ignore());

        CreateMap<UpdateSuperCategoryDto, SuperCategory>()
            .ForMember(dest => dest.SuperCategoryId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Categories, opt => opt.Ignore());
    }
}
