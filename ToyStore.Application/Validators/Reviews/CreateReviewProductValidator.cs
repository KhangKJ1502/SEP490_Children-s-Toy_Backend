using FluentValidation;
using Microsoft.AspNetCore.Http;
using ToyStore.Application.DTOs.Reviews;

namespace ToyStore.Application.Validators.Reviews;

/// <summary>
/// Validator kiểm tra tính hợp lệ của dữ liệu đầu vào khi khách hàng tạo mới đánh giá sản phẩm (CreateReviewProductDto).
/// </summary>
public class CreateReviewProductValidator : AbstractValidator<CreateReviewProductDto>
{
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Khởi tạo các quy tắc kiểm tra cho việc tạo mới đánh giá:
    /// - OrderId, ProductId: Bắt buộc lớn hơn 0.
    /// - Rating: Điểm đánh giá phải từ 1 đến 5 sao.
    /// - Comment: Tối đa 500 ký tự (nếu có nhập).
    /// - Images: Tối đa 3 hình ảnh, mỗi ảnh <= 5MB và thuộc định dạng .jpg, .jpeg, .png, .webp.
    /// </summary>
    public CreateReviewProductValidator()
    {
        // Kiểm tra mã đơn hàng
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("OrderId must be greater than 0.");

        // Kiểm tra mã sản phẩm
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId must be greater than 0.");

        // Kiểm tra số sao đánh giá (1-5 sao)
        RuleFor(x => x.Rating)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("Rating must be between 1 and 5.");

        // Kiểm tra độ dài nội dung bình luận
        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));

        // Kiểm tra số lượng ảnh tối đa
        RuleFor(x => x.Images)
            .Must(images => images == null || images.Count <= 3)
            .WithMessage("A maximum of 3 images can be uploaded.");

        // Kiểm tra dung lượng và định dạng từng tệp ảnh tải lên
        RuleForEach(x => x.Images)
            .ChildRules(image =>
            {
                image.RuleFor(i => i)
                    .Must(BeValidFileSize)
                    .WithMessage("Each image must not exceed 5MB.");

                image.RuleFor(i => i)
                    .Must(BeValidExtension)
                    .WithMessage("Only .jpg, .jpeg, .png, and .webp formats are allowed.");
            })
            .When(x => x.Images != null && x.Images.Any());
    }

    /// <summary>
    /// Kiểm tra kích thước file có nhỏ hơn hoặc bằng 5MB hay không.
    /// </summary>
    private bool BeValidFileSize(IFormFile file)
    {
        if (file == null) return true;
        return file.Length <= MaxFileSize;
    }

    /// <summary>
    /// Kiểm tra phần mở rộng của file có nằm trong danh sách định dạng ảnh cho phép hay không.
    /// </summary>
    private bool BeValidExtension(IFormFile file)
    {
        if (file == null) return true;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedExtensions.Contains(ext);
    }
}
