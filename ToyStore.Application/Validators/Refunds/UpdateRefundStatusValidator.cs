using FluentValidation;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Validators.Refunds;

/// <summary>
/// Validator kiểm tra tính hợp lệ của dữ liệu khi Quản trị viên/Nhân viên cập nhật trạng thái yêu cầu hoàn tiền (UpdateRefundStatusDto).
/// </summary>
public class UpdateRefundStatusValidator : AbstractValidator<UpdateRefundStatusDto>
{
    /// <summary>
    /// Khởi tạo các quy tắc kiểm tra cho cập nhật trạng thái hoàn tiền:
    /// - Status: Bắt buộc thuộc danh sách các trạng thái hợp lệ trong RefundStatuses.
    /// - RejectReason: Bắt buộc khi từ chối (RefundRejected), tối đa 500 ký tự.
    /// - ReturnShippingFeeNote: Tối đa 500 ký tự.
    /// - ShippingOrderCode, ReturnShippingOrderCode: Đúng định dạng mã vận đơn GHN (5-20 ký tự chữ hoa/số).
    /// - Image URLs: Tối đa 500 ký tự.
    /// - InspectionNote, AdminNote: Tối đa 500 ký tự.
    /// </summary>
    public UpdateRefundStatusValidator()
    {
        // Kiểm tra tính hợp lệ của tên trạng thái
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => 
                status == RefundStatuses.Requested ||
                status == RefundStatuses.Approved || 
                status == RefundStatuses.Rejected || 
                status == RefundStatuses.PickupCreated ||
                status == RefundStatuses.Shipping ||
                status == RefundStatuses.Received ||
                status == RefundStatuses.InspectionPending ||
                status == RefundStatuses.Completed ||
                status == RefundStatuses.Cancelled ||
                status == RefundStatuses.ReturnShipmentCreated ||
                status == RefundStatuses.ReturningToCustomer ||
                status == RefundStatuses.ReturnedToCustomer ||
                status == RefundStatuses.ReturnToCustomerFailed)
            .WithMessage("Status name is invalid.");

        // Kiểm tra lý do từ chối khi chuyển sang Rejected
        RuleFor(x => x.RejectReason)
            .NotEmpty().WithMessage("RejectReason is required when status is RefundRejected.")
            .When(x => x.Status == RefundStatuses.Rejected)
            .MaximumLength(500).WithMessage("RejectReason must not exceed 500 characters.");

        // Kiểm tra ghi chú phí ship hoàn
        RuleFor(x => x.ReturnShippingFeeNote)
            .MaximumLength(500).WithMessage("ReturnShippingFeeNote must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ReturnShippingFeeNote));

        // Kiểm tra định dạng mã vận đơn giao hàng GHN
        RuleFor(x => x.ShippingOrderCode)
            .Matches(@"^[A-Z0-9]{5,20}$").WithMessage("Shipping Order Code must be uppercase alphanumeric (5 to 20 characters) and contain no spaces or special symbols.")
            .When(x => !string.IsNullOrEmpty(x.ShippingOrderCode));

        // Kiểm tra định dạng mã vận đơn trả hàng GHN
        RuleFor(x => x.ReturnShippingOrderCode)
            .Matches(@"^[A-Z0-9]{5,20}$").WithMessage("Return Shipping Order Code must be uppercase alphanumeric (5 to 20 characters) and contain no spaces or special symbols.")
            .When(x => !string.IsNullOrEmpty(x.ReturnShippingOrderCode));

        // Kiểm tra độ dài URL ảnh giao nhận hàng hoàn
        RuleFor(x => x.ReturnDeliveryImageUrl)
            .MaximumLength(500).WithMessage("ReturnDeliveryImageUrl must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ReturnDeliveryImageUrl));

        // Kiểm tra độ dài URL ảnh gửi trả hàng cho khách
        RuleFor(x => x.ReturnToCustomerImageUrl)
            .MaximumLength(500).WithMessage("ReturnToCustomerImageUrl must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ReturnToCustomerImageUrl));

        // Kiểm tra độ dài ghi chú kiểm tra hàng hoàn
        RuleFor(x => x.InspectionNote)
            .MaximumLength(500).WithMessage("InspectionNote must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.InspectionNote));

        // Kiểm tra độ dài ghi chú nội bộ của admin
        RuleFor(x => x.AdminNote)
            .MaximumLength(500).WithMessage("AdminNote must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.AdminNote));
    }
}
