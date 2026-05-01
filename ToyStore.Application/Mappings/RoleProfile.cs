using AutoMapper;
using ToyStore.Application.DTOs.Roles;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class RoleProfile : Profile
{
    public RoleProfile()
    {
        CreateMap<Role, RoleDto>();
    }
}
