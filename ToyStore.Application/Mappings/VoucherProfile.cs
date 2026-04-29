using AutoMapper;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Vouchers;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper cho voucher.
/// </summary>
public class VoucherProfile : Profile
{
    public VoucherProfile()
    {
        CreateMap<VoucherModel, VoucherDto>();

        CreateMap<VoucherModel, VoucherListDto>();

        CreateMap<CreateVoucherDto, VoucherModel>()
            .ForMember(dest => dest.VoucherId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UsedQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateVoucherDto, VoucherModel>()
            .ForAllMembers(opt =>
                opt.Condition((_, _, srcMember) => srcMember is not null));
    }
}
