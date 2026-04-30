namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Render noi dung template bang cach thay the cac token {{Key}} bang gia tri thuc.
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// Thay the tat ca cac token {{Key}} trong template bang gia tri trong placeholders.
    /// Token khong co trong placeholders duoc giu nguyen.
    /// </summary>
    string Render(string template, IReadOnlyDictionary<string, string> placeholders);
}
