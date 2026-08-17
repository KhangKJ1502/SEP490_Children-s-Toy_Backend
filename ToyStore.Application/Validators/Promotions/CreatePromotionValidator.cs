using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

/// <summary>
/// Validator kiểm tra tính hợp lệ của dữ liệu đầu vào khi tạo mới hoặc validate tổng thể chương trình khuyến mãi (CreatePromotionDto).
/// </summary>
public class CreatePromotionValidator : AbstractValidator<CreatePromotionDto>
{
    /// <summary>
    /// Khởi tạo các quy tắc kiểm tra (Validation Rules) cho CreatePromotionDto:
    /// - PromotionName: Bắt buộc, tối đa 200 ký tự.
    /// - PromotionType: Bắt buộc ("DISCOUNT" hoặc "FLASH_SALE"), tối đa 50 ký tự.
    /// - Description: Tối đa 1000 ký tự nếu có.
    /// - StartDate: Bắt buộc, nếu trạng thái là Scheduled (khi tạo mới) thì phải cách thời điểm hiện tại ít nhất 10 phút.
    /// - EndDate: Bắt buộc, phải sau StartDate.
    /// - Status: Bắt buộc, tối đa 50 ký tự.
    /// - Priority: Phải lớn hơn hoặc bằng 0.
    /// - Validate lặp cho từng item trong ProductPromotions và PromotionTimeSlots.
    /// - Các TimeSlot phải nằm trọn vẹn trong khoảng [StartDate, EndDate] của chương trình và không được chồng lấn thời gian (overlap).
    /// </summary>
    public CreatePromotionValidator()
    {
        // Kiểm tra tên chương trình khuyến mãi
        RuleFor(x => x.PromotionName)
            .NotEmpty().WithMessage("Tên chương trình khuyến mãi là bắt buộc.")
            .MaximumLength(200).WithMessage("Tên chương trình khuyến mãi không được vượt quá 200 ký tự.");

        // Kiểm tra loại khuyến mãi
        RuleFor(x => x.PromotionType)
            .NotEmpty().WithMessage("Loại khuyến mãi là bắt buộc.")
            .MaximumLength(50).WithMessage("Loại khuyến mãi không được vượt quá 50 ký tự.");

        // Kiểm tra mô tả chi tiết
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // Kiểm tra ngày bắt đầu: nếu là Scheduled khi tạo mới, phải cách thời điểm hiện tại ít nhất 10 phút
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Ngày bắt đầu là bắt buộc.")
            .Must(d => d >= DateTime.UtcNow.AddMinutes(9))
            .When((x, ctx) => x.Status == "Scheduled" && !ctx.RootContextData.ContainsKey("IsUpdate"))
            .WithMessage("Ngày bắt đầu của chương trình lên lịch phải cách hiện tại ít nhất 10 phút.");

        // Kiểm tra ngày kết thúc: phải lớn hơn ngày bắt đầu
        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Ngày kết thúc là bắt buộc.")
            .GreaterThan(x => x.StartDate).WithMessage("Ngày kết thúc phải lớn hơn ngày bắt đầu.");

        // Kiểm tra trạng thái
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Trạng thái là bắt buộc.")
            .MaximumLength(50).WithMessage("Trạng thái không được vượt quá 50 ký tự.");

        // Kiểm tra độ ưu tiên
        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Độ ưu tiên phải lớn hơn hoặc bằng 0.");

        // Validate từng sản phẩm giảm giá trong danh sách
        RuleForEach(x => x.ProductPromotions)
            .SetValidator(new CreateProductPromotionValidator());

        // Validate từng khung giờ Flash Sale trong danh sách
        RuleForEach(x => x.PromotionTimeSlots)
            .SetValidator(new CreatePromotionTimeSlotValidator());

        // Đảm bảo tất cả các khung giờ Flash Sale phải nằm trọn trong thời gian diễn ra chương trình
        RuleFor(x => x.PromotionTimeSlots)
            .Must((dto, slots) => slots == null || slots.All(s => s.StartAt >= dto.StartDate && s.EndAt <= dto.EndDate))
            .WithMessage("Tất cả các khung giờ Flash Sale phải nằm trong khoảng thời gian diễn ra chương trình khuyến mãi.")
            .When(x => x.PromotionTimeSlots != null && x.StartDate != default && x.EndDate != default);

        // Đảm bảo các khung giờ Flash Sale không bị chồng chéo thời gian lên nhau
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
