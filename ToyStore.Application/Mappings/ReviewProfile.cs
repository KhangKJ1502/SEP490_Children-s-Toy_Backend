using AutoMapper;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper Profile chuyển đổi qua lại giữa các thực thể Đánh giá sản phẩm (ReviewProduct, ReviewProductImage, StaffReviewProductReply, ReviewModerationLog) và các DTO tương ứng.
/// </summary>
public class ReviewProfile : Profile
{
    /// <summary>
    /// Khởi tạo các ánh xạ (Mapping) cho tính năng Đánh giá sản phẩm:
    /// - ReviewProduct -> ReviewProductListDto (Công khai: chỉ lấy ảnh Approved và reply chưa xóa)
    /// - ReviewProduct -> AdminReviewListDto (Danh sách quản trị kèm số lượng ảnh/reply)
    /// - ReviewProduct -> AdminReviewDetailDto (Chi tiết quản trị kèm toàn bộ ảnh, reply và lịch sử kiểm duyệt)
    /// - ReviewProduct -> ReviewProductDto (Chi tiết trả về cho khách hàng)
    /// - ReviewProductImage -> ReviewImageDto
    /// - StaffReviewProductReply -> StaffReplyDto
    /// - ReviewModerationLog -> ModerationLogDto
    /// </summary>
    public ReviewProfile()
    {
        // Ánh xạ danh sách đánh giá công khai (Public)
        CreateMap<ReviewProduct, ReviewProductListDto>()
            .ForMember(dest => dest.ReviewerName, opt => opt.MapFrom(src => src.Account.AccountName))
            .ForMember(dest => dest.ReviewerAvatarUrl, opt => opt.MapFrom(src => src.Account.ImageUrl))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.ReviewProductImages.Where(i => !i.IsDeleted && i.ModerationStatus == "Approved")))
            .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.StaffReviewProductReplies.Where(r => !r.IsDeleted)));

        // Ánh xạ danh sách đánh giá quản trị (Admin List)
        CreateMap<ReviewProduct, AdminReviewListDto>()
            .ForMember(dest => dest.AccountEmail, opt => opt.MapFrom(src => src.Account.Email))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))
            .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order.OrderCode))
            .ForMember(dest => dest.ImagesCount, opt => opt.MapFrom(src => src.ReviewProductImages.Count(i => !i.IsDeleted)))
            .ForMember(dest => dest.RepliesCount, opt => opt.MapFrom(src => src.StaffReviewProductReplies.Count(r => !r.IsDeleted)));

        // Ánh xạ chi tiết đánh giá quản trị (Admin Detail)
        CreateMap<ReviewProduct, AdminReviewDetailDto>()
            .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account.AccountName))
            .ForMember(dest => dest.AccountEmail, opt => opt.MapFrom(src => src.Account.Email))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))
            .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order.OrderCode))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.ReviewProductImages.Where(i => !i.IsDeleted)))
            .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.StaffReviewProductReplies.Where(r => !r.IsDeleted)))
            .ForMember(dest => dest.ModerationLogs, opt => opt.MapFrom(src => src.ReviewModerationLogs.OrderByDescending(l => l.CreatedAt)));

        // Ánh xạ chi tiết đánh giá trả về cho khách hàng sau khi tạo/sửa
        CreateMap<ReviewProduct, ReviewProductDto>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.ReviewProductImages.Where(i => !i.IsDeleted)))
            .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.StaffReviewProductReplies.Where(r => !r.IsDeleted)));

        // Ánh xạ hình ảnh đánh giá
        CreateMap<ReviewProductImage, ReviewImageDto>();
        
        // Ánh xạ phản hồi của nhân viên
        CreateMap<StaffReviewProductReply, StaffReplyDto>()
            .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.Staff.AccountName));

        // Ánh xạ nhật ký kiểm duyệt
        CreateMap<ReviewModerationLog, ModerationLogDto>()
            .ForMember(dest => dest.ModeratedByName, opt => opt.MapFrom(src => src.ModeratedByNavigation != null ? src.ModeratedByNavigation.AccountName : null));
    }
}
