using AutoMapper;
using ToyStore.Application.DTOs.Accounts;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class AccountProfile : Profile
{
    public AccountProfile()
    {
        CreateMap<Account, AccountDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName));

        CreateMap<Account, AccountListDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName));

        CreateMap<CreateAccountDto, Account>();
    }
}
