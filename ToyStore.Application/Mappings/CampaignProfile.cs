using AutoMapper;
using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Mappings;

public class CampaignProfile : Profile
{
    public CampaignProfile()
    {
        CreateMap<Campaign, CampaignListDto>()
            .ForMember(dest => dest.ScheduledAt, opt => opt.MapFrom(src =>
                src.ScheduledAt ?? (src.CampaignSchedule != null ? (DateTime?)src.CampaignSchedule.ScheduledAt : null)));
        CreateMap<Campaign, CampaignDto>()
            .ForMember(dest => dest.Targets,           opt => opt.MapFrom(src => src.CampaignTargets))
            .ForMember(dest => dest.Stat,              opt => opt.MapFrom(src => src.CampaignStat))
            .ForMember(dest => dest.ResolvedReference, opt => opt.Ignore())
            .ForMember(dest => dest.ScheduledAt, opt => opt.MapFrom(src =>
                src.ScheduledAt ?? (src.CampaignSchedule != null ? (DateTime?)src.CampaignSchedule.ScheduledAt : null)))
            .ForMember(dest => dest.CreatedByAccountName, opt => opt.MapFrom(src => src.CreatedByAccount != null ? src.CreatedByAccount.AccountName : null))
            .ForMember(dest => dest.SubmittedByAccountName, opt => opt.MapFrom(src => src.SubmittedByAccount != null ? src.SubmittedByAccount.AccountName : null))
            .ForMember(dest => dest.ReviewedByAccountName, opt => opt.MapFrom(src => src.ReviewedByAccount != null ? src.ReviewedByAccount.AccountName : null));

        CreateMap<CampaignTarget, CampaignTargetDto>();
        CreateMap<CampaignStat, CampaignStatDto>();
    }
}
