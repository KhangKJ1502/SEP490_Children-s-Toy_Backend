using AutoMapper;
using ToyStore.Application.DTOs.Vouchers;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper Profile chuyển đổi qua lại giữa Entity Voucher và các DTO tương ứng.
/// </summary>
public class VoucherProfile : Profile
{
    /// <summary>
    /// Khởi tạo các ánh xạ (Mapping) cho Voucher:
    /// - Voucher -> VoucherDto (Chi tiết)
    /// - Voucher -> VoucherListDto (Danh sách rút gọn)
    /// - CreateVoucherDto -> Voucher (Tạo mới)
    /// - UpdateVoucherDto -> Voucher (Cập nhật từng phần với PreCondition an toàn)
    /// </summary>
    public VoucherProfile()
    {
        // Ánh xạ từ thực thể Voucher sang DTO chi tiết đầy đủ
        CreateMap<Voucher, VoucherDto>();

        // Ánh xạ từ thực thể Voucher sang DTO rút gọn cho danh sách hiển thị
        CreateMap<Voucher, VoucherListDto>();

        // Ánh xạ từ DTO tạo mới sang thực thể Voucher
        // Bỏ qua việc ghi đè các trường do hệ thống tự tính: ID, người tạo, ngày tạo, ngày sửa
        // Khởi tạo UsedQuantity = 0 và IsDeleted = false
        CreateMap<CreateVoucherDto, Voucher>()
            .ForMember(dest => dest.VoucherId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UsedQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Ánh xạ từ DTO cập nhật (UpdateVoucherDto) sang thực thể Voucher hiện tại (Partial Update)
        // Cấu hình PreCondition cho các kiểu dữ liệu Nullable value type (decimal?, int?, short?, DateTime?, bool?)
        // nhằm đảm bảo nếu property trong DTO là null thì AutoMapper không tự động gán giá trị mặc định (0, false, default Date) vào Entity
        CreateMap<UpdateVoucherDto, Voucher>()
            .ForMember(d => d.DiscountValue, o => { o.PreCondition(s => s.DiscountValue.HasValue); o.MapFrom(s => s.DiscountValue); })
            .ForMember(d => d.MaxDiscountCap, o => { o.PreCondition(s => s.MaxDiscountCap.HasValue); o.MapFrom(s => s.MaxDiscountCap); })
            .ForMember(d => d.MinOrderAmount, o => { o.PreCondition(s => s.MinOrderAmount.HasValue); o.MapFrom(s => s.MinOrderAmount); })
            .ForMember(d => d.TotalQuantity, o => { o.PreCondition(s => s.TotalQuantity.HasValue); o.MapFrom(s => s.TotalQuantity); })
            .ForMember(d => d.MaxUsagePerUser, o => { o.PreCondition(s => s.MaxUsagePerUser.HasValue); o.MapFrom(s => s.MaxUsagePerUser); })
            .ForMember(d => d.StartDate, o => { o.PreCondition(s => s.StartDate.HasValue); o.MapFrom(s => s.StartDate); })
            .ForMember(d => d.EndDate, o => { o.PreCondition(s => s.EndDate.HasValue); o.MapFrom(s => s.EndDate); })
            .ForMember(d => d.IsDeleted, o => { o.PreCondition(s => s.IsDeleted.HasValue); o.MapFrom(s => s.IsDeleted); })
            // Đối với các trường chuỗi/object khác, bỏ qua không ghi đè nếu giá trị nguồn là null
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
