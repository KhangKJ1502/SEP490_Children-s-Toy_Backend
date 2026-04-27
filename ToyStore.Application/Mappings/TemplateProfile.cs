using AutoMapper;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Templates;

namespace ToyStore.Application.Mappings;

public class TemplateProfile : Profile
{
    public TemplateProfile()
    {
        CreateMap<TemplateModel, TemplateListDto>();
    }
}