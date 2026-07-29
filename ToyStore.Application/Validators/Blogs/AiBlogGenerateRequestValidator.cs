using FluentValidation;
using ToyStore.Application.DTOs.Blogs;

namespace ToyStore.Application.Validators.Blogs;

/// <summary>
/// Validator cho AiBlogGenerateRequest — kiểm tra dữ liệu cơ bản trước khi gửi sang AI service.
/// </summary>
public class AiBlogGenerateRequestValidator : AbstractValidator<AiBlogGenerateRequest>
{
    public AiBlogGenerateRequestValidator()
    {
        // ═══ TIÊU ĐỀ BÀI VIẾT ═══
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        // ═══ CẤU TRÚC PROMPT ═══
        RuleFor(x => x.PromptStructure)
            .NotEmpty().WithMessage("Prompt structure is required.")
            .MinimumLength(10).WithMessage("Prompt structure must be at least 10 characters.")
            .MaximumLength(5000).WithMessage("Prompt structure must not exceed 5000 characters.");

        // ═══ MÔ TẢ (optional) ═══
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        // ═══ DANH MỤC BLOG ═══
        RuleFor(x => x.DefaultCategoryId)
            .GreaterThan(0).WithMessage("Blog category is required.")
            .LessThanOrEqualTo((int)short.MaxValue).WithMessage("Blog category ID is invalid.");

        // ═══ BLOG POST ID (optional — dùng khi regenerate bài đã có) ═══
        RuleFor(x => x.BlogPostId)
            .GreaterThan(0).WithMessage("Blog post ID must be greater than 0.")
            .When(x => x.BlogPostId.HasValue);

        // ═══ ACTION (optional) ═══
        RuleFor(x => x.Action)
            .MaximumLength(50).WithMessage("Action must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Action));

        // ═══ TONE (optional) ═══
        RuleFor(x => x.DefaultTone)
            .MaximumLength(50).WithMessage("Default tone must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.DefaultTone));

        // ═══ SOURCE CONTENT (optional) ═══
        RuleFor(x => x.SourceContent)
            .MaximumLength(20000).WithMessage("Source content must not exceed 20,000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.SourceContent));
    }
}
