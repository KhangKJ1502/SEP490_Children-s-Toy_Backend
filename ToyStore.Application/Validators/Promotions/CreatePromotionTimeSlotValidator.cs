using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

/// <summary>
/// Validator kiểm tra tính hợp lệ của dữ liệu khung giờ Flash Sale (CreatePromotionTimeSlotDto).
/// </summary>
public class CreatePromotionTimeSlotValidator : AbstractValidator<CreatePromotionTimeSlotDto>
{
    /// <summary>
    /// Khởi tạo các quy tắc kiểm tra cho khung giờ Flash Sale:
    /// - StartAt: Bắt buộc, nếu là Scheduled khi tạo mới phải cách hiện tại ít nhất 10 phút.
    /// - EndAt: Bắt buộc, phải sau StartAt (nếu là Scheduled thì phải cách StartAt tối thiểu 10 phút).
    /// - Status: Bắt buộc, chỉ chấp nhận "Active", "Scheduled", hoặc "Expired".
    /// - Validate lặp cho từng sản phẩm trong PromotionProductSlots.
    /// </summary>
    public CreatePromotionTimeSlotValidator()
    {
        // Kiểm tra thời điểm bắt đầu khung giờ
        RuleFor(x => x.StartAt)
            .NotEmpty().WithMessage("Start time is required.")
            .Must(d => d >= DateTime.UtcNow.AddMinutes(9))
            .When((x, ctx) => x.Status == "Scheduled" && !ctx.RootContextData.ContainsKey("IsUpdate"))
            .WithMessage("Scheduled start time must be at least 10 minutes in the future.");

        // Kiểm tra thời điểm kết thúc khung giờ: luôn phải sau StartAt
        RuleFor(x => x.EndAt)
            .NotEmpty().WithMessage("End time is required.")
            .GreaterThan(x => x.StartAt).WithMessage("End time must be after start time.");

        // Đối với khung giờ mới lên lịch (Scheduled), khoảng cách giữa StartAt và EndAt tối thiểu 10 phút
        RuleFor(x => x.EndAt)
            .GreaterThan(x => x.StartAt.AddMinutes(9))
            .When(x => x.Status == "Scheduled")
            .WithMessage("End time must be at least 10 minutes after start time.");

        // Kiểm tra giá trị trạng thái khung giờ
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Time slot status is required.")
            .Must(s => s == "Active" || s == "Scheduled" || s == "Expired")
            .WithMessage("Time slot status must be 'Active', 'Scheduled', or 'Expired'.");

        // Validate lặp cho từng sản phẩm trong khung giờ này
        RuleForEach(x => x.PromotionProductSlots)
            .SetValidator(new CreatePromotionProductSlotValidator());
    }
}
