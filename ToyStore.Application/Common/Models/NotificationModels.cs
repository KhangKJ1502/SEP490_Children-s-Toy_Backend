namespace ToyStore.Application.Common.Models;

public class TemplateModel
{
    public short TemplateId { get; set; }

    public string TemplateCode { get; set; } = string.Empty;

    public string TitleTemplate { get; set; } = string.Empty;

    public string MessageTemplate { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}