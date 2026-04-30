using AutoMapper;
using ToyStore.Application.DTOs.Brands;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class BrandProfile : Profile
{
    public BrandProfile()
    {
        CreateMap<Brand, BrandListDto>()
            .ForMember(
                dest => dest.Status,
                opt => opt.MapFrom(src => src.IsDeleted ? "Inactive" : "Active"));

        CreateMap<BrandListDto, Brand>()
            .ForMember(
                dest => dest.IsDeleted,
                opt => opt.MapFrom(src => string.Equals(src.Status, "Inactive", StringComparison.OrdinalIgnoreCase)));
    }
}
