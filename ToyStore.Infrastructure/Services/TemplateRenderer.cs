using System.Text.RegularExpressions;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Thay the cac token {{Key}} trong template bang gia tri thuc tu placeholders.
/// Token khong tim thay giu nguyen trong output.
/// </summary>
public partial class TemplateRenderer : ITemplateRenderer
{
    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
    private static partial Regex TokenPattern();

    public string Render(string template, IReadOnlyDictionary<string, string> placeholders)
    {
        if (string.IsNullOrEmpty(template)) return template;

        return TokenPattern().Replace(template, match =>
        {
            var token = match.Value;
            return placeholders.TryGetValue(token, out var value) ? value : token;
        });
    }
}
