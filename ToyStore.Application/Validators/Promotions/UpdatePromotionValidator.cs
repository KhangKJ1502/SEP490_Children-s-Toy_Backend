using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

/// <summary>
/// Validator kiểm tra tính hợp lệ của dữ liệu đầu vào khi cập nhật một phần hoặc toàn bộ chương trình khuyến mãi (UpdatePromotionDto).
/// </summary>
public class UpdatePromotionValidator : AbstractValidator<UpdatePromotionDto>
{
    /// <summary>
    /// Khởi tạo các quy tắc kiểm tra cho cập nhật (chỉ validate các trường có giá trị khác null):
    /// - PromotionName: Nếu có thì không được rỗng và tối đa 200 ký tự.
    /// - PromotionType: Nếu có thì không được rỗng và tối đa 50 ký tự.
    /// - Description: Tối đa 1000 ký tự.
    /// - StartDate & EndDate: Nếu có cả 2 thì EndDate phải sau StartDate.
    /// - Status: Tối đa 50 ký tự.
    /// - Priority: Phải >= 0.
    /// - Validate lặp cho ProductPromotions và PromotionTimeSlots nếu có gửi lên.
    /// - Kiểm tra TimeSlots nằm trong khoảng thời gian và không chồng lấn.
    /// </summary>
    public UpdatePromotionValidator()
    {
        // Kiểm tra tên chương trình khuyến mãi khi được cập nhật
        RuleFor(x => x.PromotionName)
            .NotEmpty().WithMessage("Promotion name must not be empty.")
            .MaximumLength(200).WithMessage("Promotion name must not exceed 200 characters.")
            .When(x => x.PromotionName != null);

        // Kiểm tra loại khuyến mãi khi được cập nhật
        RuleFor(x => x.PromotionType)
            .NotEmpty().WithMessage("Promotion type must not be empty.")
            .MaximumLength(50).WithMessage("Promotion type must not exceed 50 characters.")
            .When(x => x.PromotionType != null);

        // Kiểm tra mô tả chi tiết khi được cập nhật
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description != null);

        // Kiểm tra ngày bắt đầu khi được cập nhật
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date must not be empty.")
            .When(x => x.StartDate.HasValue);

        // Kiểm tra ngày kết thúc: phải lớn hơn ngày bắt đầu khi cả 2 cùng được cập nhật
        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date must not be empty.")
            .GreaterThan(x => x.StartDate!.Value).WithMessage("End date must be after start date.")
            .When(x => x.EndDate.HasValue && x.StartDate.HasValue);

        // Kiểm tra trạng thái khi được cập nhật
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status must not be empty.")
            .MaximumLength(50).WithMessage("Status must not exceed 50 characters.")
            .When(x => x.Status != null);

        // Kiểm tra độ ưu tiên khi được cập nhật
        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Priority must be greater than or equal to 0.")
            .When(x => x.Priority.HasValue);

        // Validate từng sản phẩm giảm giá khi danh sách sản phẩm được cập nhật
        RuleForEach(x => x.ProductPromotions)
            .SetValidator(new CreateProductPromotionValidator())
            .When(x => x.ProductPromotions != null);

        // Validate từng khung giờ Flash Sale khi danh sách khung giờ được cập nhật
        RuleForEach(x => x.PromotionTimeSlots)
            .SetValidator(new CreatePromotionTimeSlotValidator())
            .When(x => x.PromotionTimeSlots != null);

        // Đảm bảo tất cả các khung giờ Flash Sale phải nằm trong khoảng thời gian diễn ra chương trình
        RuleFor(x => x.PromotionTimeSlots)
            .Must((dto, slots) => slots == null || slots.All(s => s.StartAt >= dto.StartDate && s.EndAt <= dto.EndDate))
            .WithMessage("All Flash Sale time slots must be within the promotion duration.")
            .When(x => x.PromotionTimeSlots != null && x.StartDate.HasValue && x.EndDate.HasValue);

        // Đảm bảo các khung giờ Flash Sale không bị chồng lấn thời gian với nhau
        RuleFor(x => x.PromotionTimeSlots)
            .Must((dto, slots) => 
            {
                if (slots == null || !slots.Any()) return true;
                var activeSlots = slots.Where(s => s.Status != "Expired").ToList();
                for (int i = 0; i < activeSlots.Count; i++)
                {
                    for (int j = i + 1; j < activeSlots.Count; j++)
                    {
                        if (activeSlots[i].StartAt < activeSlots[j].EndAt && activeSlots[j].StartAt < activeSlots[i].EndAt)
                            return false;
                    }
                }
                return true;
            })
            .WithMessage("Flash Sale time slots must not overlap with each other.")
            .When(x => x.PromotionTimeSlots != null);
    }
}
