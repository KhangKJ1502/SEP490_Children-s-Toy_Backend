using FluentValidation;
using ToyStore.Application.DTOs.Tracking;

namespace ToyStore.Application.Validators.Tracking;

/// <summary>
/// Validator cho TrackEventRequestDto — kiểm tra format SessionId, danh sách event,
/// AccountId (nếu có) phải > 0, và mỗi event con phải hợp lệ.
/// </summary>
public class TrackEventRequestValidator : AbstractValidator<TrackEventRequestDto>
{
    /// <summary>Danh sách EventType hợp lệ (theo spec — không phụ thuộc layer Recommendation).</summary>
    private static readonly IReadOnlySet<string> AllowedEventTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "product_view", "product_view_long", "add_to_cart", "add_to_wishlist",
        "purchase", "search", "category_browse", "review_submit", "remove_from_cart",
    };

    /// <summary>Danh sách EntityType hợp lệ.</summary>
    private static readonly IReadOnlySet<string> AllowedEntityTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "product", "category", "brand", "search", "promotion", "page",
    };

    public TrackEventRequestValidator()
    {
        // SessionId bắt buộc (FE phải gửi)
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required.")
            .MinimumLength(8).WithMessage("SessionId must be at least 8 characters.")
            .MaximumLength(128).WithMessage("SessionId must not exceed 128 characters.");

        // AccountId (nếu gửi) phải > 0 vì IDENTITY bắt đầu từ 1
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("AccountId must be greater than 0.")
            .When(x => x.AccountId.HasValue);

        // Events — không rỗng, không vượt giới hạn 100 (đủ buffer cho batch FE)
        RuleFor(x => x.Events)
            .NotNull().WithMessage("Events list is required.")
            .NotEmpty().WithMessage("Events list must contain at least 1 event.")
            .Must(events => events.Count <= 100)
            .WithMessage("Events list must not exceed 100 items per request.");

        // Validate từng event con
        RuleForEach(x => x.Events).SetValidator(new TrackEventItemValidator());
    }

    private class TrackEventItemValidator : AbstractValidator<TrackEventItemDto>
    {
        public TrackEventItemValidator()
        {
            RuleFor(x => x.EventType)
                .NotEmpty().WithMessage("EventType is required.")
                .MaximumLength(50).WithMessage("EventType must not exceed 50 characters.")
                .Must(BeAllowedEventType).WithMessage("EventType is not supported.");

            RuleFor(x => x.EntityType)
                .NotEmpty().WithMessage("EntityType is required.")
                .MaximumLength(50).WithMessage("EntityType must not exceed 50 characters.")
                .Must(BeAllowedEntityType).WithMessage("EntityType is not supported.");

            RuleFor(x => x.EntityId)
                .NotEmpty().WithMessage("EntityId is required.")
                .MaximumLength(200).WithMessage("EntityId must not exceed 200 characters.");

            RuleFor(x => x.Source).MaximumLength(100);
            RuleFor(x => x.Referrer).MaximumLength(2048);
            RuleFor(x => x.DeviceType).MaximumLength(50);

            RuleFor(x => x.DurationMs)
                .GreaterThanOrEqualTo(0).WithMessage("DurationMs must not be negative.")
                .LessThanOrEqualTo(24 * 60 * 60 * 1000) // <= 1 ngày
                .WithMessage("DurationMs is too large.")
                .When(x => x.DurationMs.HasValue);

            RuleFor(x => x.ScrollDepth)
                .InclusiveBetween((byte)0, (byte)100).WithMessage("ScrollDepth must be 0–100.")
                .When(x => x.ScrollDepth.HasValue);

            RuleFor(x => x.ClickPosition).MaximumLength(100);
            RuleFor(x => x.Metadata).MaximumLength(4000);

            // Tránh client gửi event trong tương lai > 5 phút
            RuleFor(x => x.OccurredAt)
                .Must(d => d!.Value <= DateTime.UtcNow.AddMinutes(5))
                .WithMessage("OccurredAt cannot be in the future.")
                .When(x => x.OccurredAt.HasValue);
        }

        private static bool BeAllowedEventType(string value) =>
            !string.IsNullOrWhiteSpace(value) && AllowedEventTypes.Contains(value);

        private static bool BeAllowedEntityType(string value) =>
            !string.IsNullOrWhiteSpace(value) && AllowedEntityTypes.Contains(value);
    }
}
