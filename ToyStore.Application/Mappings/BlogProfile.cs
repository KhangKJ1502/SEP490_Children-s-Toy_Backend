using AutoMapper;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class BlogProfile : Profile
{
    public BlogProfile()
    {
        CreateMap<BlogPost, BlogListDto>()
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Account.AccountName))
            .ForMember(dest => dest.ApprovedBy, opt => opt.MapFrom(src => src.ApprovedByNavigation != null ? src.ApprovedByNavigation.AccountName : null));

        CreateMap<BlogPost, BlogDetailDto>()
            .IncludeBase<BlogPost, BlogListDto>();
    }
}
