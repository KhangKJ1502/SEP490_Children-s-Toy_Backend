using AutoMapper;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class BlogProfile : Profile
{
    public BlogProfile()
    {
        CreateMap<BlogPost, BlogListDto>()
            .ForMember(dest => dest.BlogCategoryName, opt => opt.MapFrom(src => src.BlogCategory.BlogCategoriesName))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Account.AccountName))
            .ForMember(dest => dest.IsHidden, opt => opt.MapFrom(src => string.Equals(src.Status, "Hidden", StringComparison.OrdinalIgnoreCase)))
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.BlogPostStat != null ? src.BlogPostStat.LikeCount : 0))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.BlogPostStat != null ? src.BlogPostStat.CommentCount : 0))
            .ForMember(dest => dest.TotalInteraction, opt => opt.MapFrom(src => src.BlogPostStat != null ? src.BlogPostStat.LikeCount + src.BlogPostStat.CommentCount : 0));

        CreateMap<BlogPost, BlogDetailDto>()
            .IncludeBase<BlogPost, BlogListDto>();

        CreateMap<BlogCommentViolationCount, BlogReviewPermissionDto>()
            .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account.AccountName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Account.Email))
            .ForMember(dest => dest.AccountImageUrl, opt => opt.MapFrom(src => src.Account.ImageUrl))
            .ForMember(dest => dest.UnbannedByName, opt => opt.MapFrom(src => src.UnbannedByNavigation != null ? src.UnbannedByNavigation.AccountName : null));
    }
}
