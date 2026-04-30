using AutoMapper;
using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs.Templates;

namespace ToyStore.Application.Mappings;

public class TemplateProfile : Profile
{
    public TemplateProfile()
    {
        CreateMap<Template, TemplateListDto>();
        CreateMap<CreateTemplateDto, Template>();
        CreateMap<UpdateTemplateDto, Template>();
    }
}