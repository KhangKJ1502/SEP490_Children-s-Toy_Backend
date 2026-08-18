using FluentValidation;
using ToyStore.Application.DTOs.Refunds;
using System;

namespace ToyStore.Application.Validators.Refunds;

/// <summary>
/// Validator kiểm tra tính hợp lệ của dữ liệu đầu vào khi khách hàng tạo yêu cầu hoàn tiền mới (CreateRefundDto).
/// </summary>
public class CreateRefundValidator : AbstractValidator<CreateRefundDto>
{
    /// <summary>
    /// Khởi tạo các quy tắc xác thực cho CreateRefundDto:
    /// - OrderId: Bắt buộc lớn hơn 0.
    /// - RefundReasonId: Bắt buộc lớn hơn 0.
    /// - ReasonDetails: Tối đa 500 ký tự.
    /// - Images: Tối thiểu 1 hình ảnh bằng chứng, tối đa 5 hình ảnh và mỗi URL phải hợp lệ.
    /// </summary>
    public CreateRefundValidator()
    {
        // Kiểm tra mã đơn hàng
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("OrderId is required and must be greater than 0.");

        // Kiểm tra mã lý do hoàn tiền
        RuleFor(x => x.RefundReasonId)
            .GreaterThan((byte)0).WithMessage("RefundReasonId is required.");

        // Kiểm tra độ dài lý do chi tiết
        RuleFor(x => x.ReasonDetails)
            .MaximumLength(500).WithMessage("ReasonDetails must not exceed 500 characters.");

        // Kiểm tra số lượng và định dạng URL ảnh bằng chứng
        RuleFor(x => x.Images)
            .NotEmpty().WithMessage("At least one evidence image is required.")
            .Must(x => x == null || x.Count <= 5).WithMessage("A maximum of 5 evidence photos is allowed.")
            .ForEach(image => image.Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Image URL is not valid."));
    }
}
