using AutoMapper;
using ToyStore.Application.DTOs.Customers;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Account, CustomerListDto>();

        CreateMap<Account, CustomerDetailDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName))
            .ForMember(dest => dest.SexName, opt => opt.MapFrom(src => src.Sex != null ? src.Sex.SexName : null));
    }
}
