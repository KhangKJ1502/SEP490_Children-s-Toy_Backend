namespace ToyStore.Application.DTOs.Templates;

public class TemplateListDto
{
    public short TemplateId { get; set; }

    public string TemplateCode { get; set; } = string.Empty;

    public string UsageScope { get; set; } = string.Empty;

    public string TitleTemplate { get; set; } = string.Empty;

    public string MessageTemplate { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsUsed { get; set; }
}