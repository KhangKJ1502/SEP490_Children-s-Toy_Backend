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
            .NotEmpty().WithMessage("Thời điểm bắt đầu khung giờ là bắt buộc.")
            .Must(d => d >= DateTime.UtcNow.AddMinutes(9))
            .When((x, ctx) => x.Status == "Scheduled" && !ctx.RootContextData.ContainsKey("IsUpdate"))
            .WithMessage("Thời điểm bắt đầu của khung giờ lên lịch phải cách hiện tại ít nhất 10 phút.");

        // Kiểm tra thời điểm kết thúc khung giờ: luôn phải sau StartAt
        RuleFor(x => x.EndAt)
            .NotEmpty().WithMessage("Thời điểm kết thúc khung giờ là bắt buộc.")
            .GreaterThan(x => x.StartAt).WithMessage("Thời điểm kết thúc phải sau thời điểm bắt đầu.");

        // Đối với khung giờ mới lên lịch (Scheduled), khoảng cách giữa StartAt và EndAt tối thiểu 10 phút
        RuleFor(x => x.EndAt)
            .GreaterThan(x => x.StartAt.AddMinutes(9))
            .When(x => x.Status == "Scheduled")
            .WithMessage("Thời điểm kết thúc phải cách thời điểm bắt đầu ít nhất 10 phút.");

        // Kiểm tra giá trị trạng thái khung giờ
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Trạng thái khung giờ là bắt buộc.")
            .Must(s => s == "Active" || s == "Scheduled" || s == "Expired")
            .WithMessage("Trạng thái khung giờ chỉ có thể là 'Active', 'Scheduled', hoặc 'Expired'.");

        // Validate lặp cho từng sản phẩm trong khung giờ này
        RuleForEach(x => x.PromotionProductSlots)
            .SetValidator(new CreatePromotionProductSlotValidator());
    }
}
