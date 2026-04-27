namespace ToyStore.Application.DTOs.Templates;

public class UpdateTemplateDto
{
    public string TemplateCode { get; set; } = string.Empty;

    public string TitleTemplate { get; set; } = string.Empty;

    public string MessageTemplate { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}