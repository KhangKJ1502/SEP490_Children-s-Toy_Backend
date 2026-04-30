using AutoMapper;
using ToyStore.Application.DTOs.Auth;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper cho Account và Authentication.
/// </summary>
public class AccountProfile : Profile
{
    public AccountProfile()
    {
        CreateMap<Account, AccountInfoDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName));
    }
}
