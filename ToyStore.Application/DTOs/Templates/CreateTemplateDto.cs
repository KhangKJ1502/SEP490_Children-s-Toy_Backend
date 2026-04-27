namespace ToyStore.Application.DTOs.Templates;

public class CreateTemplateDto
{
    public string TemplateCode { get; set; } = string.Empty;

    public string TitleTemplate { get; set; } = string.Empty;

    public string MessageTemplate { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}