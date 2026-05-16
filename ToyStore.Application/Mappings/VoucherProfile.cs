using AutoMapper;
using ToyStore.Application.DTOs.Vouchers;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper cho voucher.
/// </summary>
public class VoucherProfile : Profile
{
    public VoucherProfile()
    {
        CreateMap<Voucher, VoucherDto>();

        CreateMap<Voucher, VoucherListDto>();

        CreateMap<CreateVoucherDto, Voucher>()
            .ForMember(dest => dest.VoucherId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UsedQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Tái cấu hình (ghi đè behavior ForAllMembers) cho các property Nullable<T> để fix lỗi tự động ép null thành default(T) của AutoMapper
        CreateMap<UpdateVoucherDto, Voucher>()
            .ForMember(d => d.DiscountValue, o => { o.PreCondition(s => s.DiscountValue.HasValue); o.MapFrom(s => s.DiscountValue); })
            .ForMember(d => d.MaxDiscountCap, o => { o.PreCondition(s => s.MaxDiscountCap.HasValue); o.MapFrom(s => s.MaxDiscountCap); })
            .ForMember(d => d.MinOrderAmount, o => { o.PreCondition(s => s.MinOrderAmount.HasValue); o.MapFrom(s => s.MinOrderAmount); })
            .ForMember(d => d.TotalQuantity, o => { o.PreCondition(s => s.TotalQuantity.HasValue); o.MapFrom(s => s.TotalQuantity); })
            .ForMember(d => d.MaxUsagePerUser, o => { o.PreCondition(s => s.MaxUsagePerUser.HasValue); o.MapFrom(s => s.MaxUsagePerUser); })
            .ForMember(d => d.StartDate, o => { o.PreCondition(s => s.StartDate.HasValue); o.MapFrom(s => s.StartDate); })
            .ForMember(d => d.EndDate, o => { o.PreCondition(s => s.EndDate.HasValue); o.MapFrom(s => s.EndDate); })
            .ForMember(d => d.IsDeleted, o => { o.PreCondition(s => s.IsDeleted.HasValue); o.MapFrom(s => s.IsDeleted); })
            // Các trường string/object vẫn dùng Condition bình thường để bỏ qua null
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
