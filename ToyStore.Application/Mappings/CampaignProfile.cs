using AutoMapper;
using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Mappings;

public class CampaignProfile : Profile
{
    public CampaignProfile()
    {
        CreateMap<Campaign, CampaignListDto>();
        CreateMap<Campaign, CampaignDto>()
            .ForMember(dest => dest.Targets,           opt => opt.MapFrom(src => src.CampaignTargets))
            .ForMember(dest => dest.Stat,              opt => opt.MapFrom(src => src.CampaignStat))
            .ForMember(dest => dest.ResolvedReference, opt => opt.Ignore());

        CreateMap<CampaignTarget, CampaignTargetDto>();
        CreateMap<CampaignStat, CampaignStatDto>();
    }
}
