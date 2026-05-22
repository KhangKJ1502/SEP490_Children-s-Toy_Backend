namespace ToyStore.Application.DTOs.Recommendations;

/// <summary>
/// Response của GET /api/recommendations?widgetCode=... — đóng gói widget + danh sách item.
/// </summary>
public class RecommendationWidgetResponseDto
{
    public string WidgetCode { get; set; } = string.Empty;
    public string WidgetName { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public List<RecommendationItemDto> Items { get; set; } = new();
}
