using AutoMapper;
using ToyStore.Application.DTOs.Shifts;
using ToyStore.Application.DTOs.Assignments;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class ShiftSchedulingProfile : Profile
{
    public ShiftSchedulingProfile()
    {
        CreateMap<ShiftTemplate, ShiftTemplateDto>();
        CreateMap<ShiftTemplate, ShiftTemplateListDto>();

        CreateMap<WorkSchedule, WorkScheduleDto>()
            .ForMember(d => d.AccountName, opt => opt.MapFrom(s => s.Account.AccountName))
            .ForMember(d => d.RoleId, opt => opt.MapFrom(s => s.Account.RoleId))
            .ForMember(d => d.ShiftName, opt => opt.MapFrom(s => s.ShiftTemplate.ShiftName))
            .ForMember(d => d.CurrentLoad, opt => opt.MapFrom(s => s.StaffShiftCapacity.CurrentLoad))
            .ForMember(d => d.MaxLoad, opt => opt.MapFrom(s => s.StaffShiftCapacity.MaxLoad))
            .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s => s.Account.ImageUrl));

        CreateMap<WorkSchedule, WorkScheduleListDto>()
            .ForMember(d => d.AccountName, opt => opt.MapFrom(s => s.Account.AccountName))
            .ForMember(d => d.RoleId, opt => opt.MapFrom(s => s.Account.RoleId))
            .ForMember(d => d.ShiftName, opt => opt.MapFrom(s => s.ShiftTemplate.ShiftName))
            .ForMember(d => d.CurrentLoad, opt => opt.MapFrom(s => s.StaffShiftCapacity.CurrentLoad))
            .ForMember(d => d.MaxLoad, opt => opt.MapFrom(s => s.StaffShiftCapacity.MaxLoad))
            .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s => s.Account.ImageUrl));

        CreateMap<OrderQueue, OrderQueueItemDto>()
            .ForMember(d => d.OrderCode, opt => opt.MapFrom(s => s.Order.OrderCode));
    }
}
