using AutoMapper;
using ToyStore.Application.DTOs.Promotions;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper Profile chuyển đổi qua lại giữa Entity Promotion, ProductPromotion, PromotionTimeSlot, PromotionProductSlot và các DTO tương ứng.
/// </summary>
public class PromotionProfile : Profile
{
    /// <summary>
    /// Khởi tạo các ánh xạ (Mapping) cho Promotion:
    /// - Promotion -> PromotionDto / PromotionListDto / CreatePromotionDto
    /// - CreatePromotionDto / UpdatePromotionDto -> Promotion
    /// - ProductPromotion <-> ProductPromotionDto / CreateProductPromotionDto
    /// - PromotionTimeSlot <-> PromotionTimeSlotDto / CreatePromotionTimeSlotDto
    /// - PromotionProductSlot <-> PromotionProductSlotDto / CreatePromotionProductSlotDto
    /// </summary>
    public PromotionProfile()
    {
        // Ánh xạ từ thực thể Promotion sang DTO chi tiết (chỉ lấy các item chưa xóa mềm)
        CreateMap<Promotion, PromotionDto>()
            .ForMember(dest => dest.ProductPromotions, opt => opt.MapFrom(src => src.ProductPromotions.Where(x => !x.IsDeleted)))
            .ForMember(dest => dest.PromotionTimeSlots, opt => opt.MapFrom(src => src.PromotionTimeSlots.Where(x => !x.IsDeleted)));

        // Ánh xạ từ thực thể Promotion sang DTO danh sách phân trang rút gọn
        CreateMap<Promotion, PromotionListDto>();

        // Ánh xạ từ thực thể Promotion sang CreatePromotionDto (dùng trong full validation khi cập nhật)
        CreateMap<Promotion, CreatePromotionDto>()
            .ForMember(dest => dest.ProductPromotions, opt => opt.MapFrom(src => src.ProductPromotions.Where(x => !x.IsDeleted)))
            .ForMember(dest => dest.PromotionTimeSlots, opt => opt.MapFrom(src => src.PromotionTimeSlots.Where(x => !x.IsDeleted)));

        // Ánh xạ từ CreatePromotionDto sang thực thể Promotion
        // Khởi tạo IsDeleted = false và bỏ qua các trường hệ thống tự sinh
        CreateMap<CreatePromotionDto, Promotion>()
            .ForMember(dest => dest.PromotionId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ProductPromotions, opt => opt.Ignore())
            .ForMember(dest => dest.PromotionTimeSlots, opt => opt.Ignore());

        // Ánh xạ cập nhật từ UpdatePromotionDto sang Promotion (chỉ ghi đè các trường khác null)
        CreateMap<UpdatePromotionDto, Promotion>()
            .ForMember(dest => dest.ProductPromotions, opt => opt.Ignore())
            .ForMember(dest => dest.PromotionTimeSlots, opt => opt.Ignore())
            .ForAllMembers(opt =>
                opt.Condition((_, _, srcMember) => srcMember is not null));

        // Ánh xạ từ ProductPromotion sang ProductPromotionDto (nạp thêm tên sản phẩm, giá gốc, tồn kho từ navigation)
        CreateMap<ProductPromotion, ProductPromotionDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : string.Empty))
            .ForMember(dest => dest.OriginalPrice, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price : 0))
            .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.Product != null ? src.Product.Quantity : 0));

        // Ánh xạ từ CreateProductPromotionDto sang ProductPromotion
        CreateMap<CreateProductPromotionDto, ProductPromotion>()
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.PromotionId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.Promotion, opt => opt.Ignore());

        CreateMap<ProductPromotion, CreateProductPromotionDto>();

        // Ánh xạ từ PromotionTimeSlot sang PromotionTimeSlotDto (lọc bỏ các sản phẩm đã xóa mềm)
        CreateMap<PromotionTimeSlot, PromotionTimeSlotDto>()
            .ForMember(dest => dest.PromotionProductSlots, opt => opt.MapFrom(src => src.PromotionProductSlots.Where(x => !x.IsDeleted)));
        CreateMap<PromotionTimeSlot, CreatePromotionTimeSlotDto>();

        // Ánh xạ từ CreatePromotionTimeSlotDto sang thực thể PromotionTimeSlot
        CreateMap<CreatePromotionTimeSlotDto, PromotionTimeSlot>()
            .ForMember(dest => dest.TimeSlotId, opt => opt.Ignore())
            .ForMember(dest => dest.PromotionId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Promotion, opt => opt.Ignore());

        // Ánh xạ từ PromotionProductSlot sang PromotionProductSlotDto (nạp thêm ảnh và giá gốc từ Product navigation)
        CreateMap<PromotionProductSlot, PromotionProductSlotDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : string.Empty))
            .ForMember(dest => dest.OriginalPrice, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price : 0))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.Product != null && src.Product.ProductImage != null ? src.Product.ProductImage.ImageUrl : null));

        // Ánh xạ từ CreatePromotionProductSlotDto sang thực thể PromotionProductSlot (khởi tạo SoldQuantity và ReservedQuantity = 0)
        CreateMap<CreatePromotionProductSlotDto, PromotionProductSlot>()
            .ForMember(dest => dest.SlotProductId, opt => opt.Ignore())
            .ForMember(dest => dest.TimeSlotId, opt => opt.Ignore())
            .ForMember(dest => dest.SoldQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.ReservedQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.TimeSlot, opt => opt.Ignore());

        CreateMap<PromotionProductSlot, CreatePromotionProductSlotDto>();
    }
}
