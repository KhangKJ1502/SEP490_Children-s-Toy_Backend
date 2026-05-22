namespace ToyStore.Application.DTOs.Tracking;

/// <summary>
/// Response của POST /api/tracking — báo cho FE biết bao nhiêu event được nhận / bị loại.
/// </summary>
public class TrackEventResponseDto
{
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
}
