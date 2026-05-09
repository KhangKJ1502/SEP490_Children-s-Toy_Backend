using AutoMapper;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        // Public List DTO
        CreateMap<ReviewProduct, ReviewProductListDto>()
            .ForMember(dest => dest.ReviewerName, opt => opt.MapFrom(src => src.Account.AccountName))
            .ForMember(dest => dest.ReviewerAvatarUrl, opt => opt.MapFrom(src => src.Account.ImageUrl))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.ReviewProductImages.Where(i => !i.IsDeleted && i.ModerationStatus == "Approved")))
            .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.StaffReviewProductReplies.Where(r => !r.IsDeleted)));

        // Admin List DTO
        CreateMap<ReviewProduct, AdminReviewListDto>()
            .ForMember(dest => dest.AccountEmail, opt => opt.MapFrom(src => src.Account.Email))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))
            .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order.OrderCode))
            .ForMember(dest => dest.ImagesCount, opt => opt.MapFrom(src => src.ReviewProductImages.Count(i => !i.IsDeleted)))
            .ForMember(dest => dest.RepliesCount, opt => opt.MapFrom(src => src.StaffReviewProductReplies.Count(r => !r.IsDeleted)));

        // Admin Detail DTO
        CreateMap<ReviewProduct, AdminReviewDetailDto>()
            .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account.AccountName))
            .ForMember(dest => dest.AccountEmail, opt => opt.MapFrom(src => src.Account.Email))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))
            .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order.OrderCode))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.ReviewProductImages.Where(i => !i.IsDeleted)))
            .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.StaffReviewProductReplies.Where(r => !r.IsDeleted)))
            .ForMember(dest => dest.ModerationLogs, opt => opt.MapFrom(src => src.ReviewModerationLogs.OrderByDescending(l => l.CreatedAt)));

        // Customer Detail DTO (After created/updated)
        CreateMap<ReviewProduct, ReviewProductDto>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.ReviewProductImages.Where(i => !i.IsDeleted)))
            .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.StaffReviewProductReplies.Where(r => !r.IsDeleted)));

        // Sub-DTOs
        CreateMap<ReviewProductImage, ReviewImageDto>();
        
        CreateMap<StaffReviewProductReply, StaffReplyDto>()
            .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.Staff.AccountName));

        CreateMap<ReviewModerationLog, ModerationLogDto>()
            .ForMember(dest => dest.ModeratedByName, opt => opt.MapFrom(src => src.ModeratedByNavigation != null ? src.ModeratedByNavigation.AccountName : null));
    }
}
