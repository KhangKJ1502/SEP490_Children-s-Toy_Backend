using AutoMapper;
using ToyStore.Application.DTOs.Addresses;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class AddressProfile : Profile
{
    public AddressProfile()
    {
        CreateMap<Address, AddressDto>()
            .ForMember(dest => dest.WardName, opt => opt.MapFrom(src => src.WardCodeNavigation != null ? src.WardCodeNavigation.WardName : null))
            .ForMember(dest => dest.DistrictName, opt => opt.MapFrom(src => src.District != null ? src.District.DistrictName : null))
            .ForMember(dest => dest.ProvinceName, opt => opt.MapFrom(src => src.Province != null ? src.Province.ProvinceName : null));

        CreateMap<Province, ProvinceOptionDto>();
        CreateMap<District, DistrictOptionDto>();
        CreateMap<Ward, WardOptionDto>();
    }
}
