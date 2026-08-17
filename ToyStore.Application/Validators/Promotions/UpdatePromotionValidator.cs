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
            .NotEmpty().WithMessage("Tên chương trình khuyến mãi không được để trống.")
            .MaximumLength(200).WithMessage("Tên chương trình khuyến mãi không được vượt quá 200 ký tự.")
            .When(x => x.PromotionName != null);

        // Kiểm tra loại khuyến mãi khi được cập nhật
        RuleFor(x => x.PromotionType)
            .NotEmpty().WithMessage("Loại khuyến mãi không được để trống.")
            .MaximumLength(50).WithMessage("Loại khuyến mãi không được vượt quá 50 ký tự.")
            .When(x => x.PromotionType != null);

        // Kiểm tra mô tả chi tiết khi được cập nhật
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.")
            .When(x => x.Description != null);

        // Kiểm tra ngày bắt đầu khi được cập nhật
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Ngày bắt đầu không được để trống.")
            .When(x => x.StartDate.HasValue);

        // Kiểm tra ngày kết thúc: phải lớn hơn ngày bắt đầu khi cả 2 cùng được cập nhật
        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Ngày kết thúc không được để trống.")
            .GreaterThan(x => x.StartDate!.Value).WithMessage("Ngày kết thúc phải lớn hơn ngày bắt đầu.")
            .When(x => x.EndDate.HasValue && x.StartDate.HasValue);

        // Kiểm tra trạng thái khi được cập nhật
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Trạng thái không được để trống.")
            .MaximumLength(50).WithMessage("Trạng thái không được vượt quá 50 ký tự.")
            .When(x => x.Status != null);

        // Kiểm tra độ ưu tiên khi được cập nhật
        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Độ ưu tiên phải lớn hơn hoặc bằng 0.")
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
            .WithMessage("Tất cả các khung giờ Flash Sale phải nằm trong khoảng thời gian diễn ra chương trình khuyến mãi.")
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
            .WithMessage("Các khung giờ Flash Sale không được trùng hoặc chồng lấn thời gian với nhau.")
            .When(x => x.PromotionTimeSlots != null);
    }
}
