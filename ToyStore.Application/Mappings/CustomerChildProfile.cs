using AutoMapper;
using ToyStore.Application.DTOs.CustomerChildren;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class CustomerChildProfile : Profile
{
    public CustomerChildProfile()
    {
        CreateMap<CustomerChild, CustomerChildDto>()
            .ForMember(dest => dest.SexName, opt => opt.MapFrom(src => src.Sex != null ? src.Sex.SexName : null));
    }
}
