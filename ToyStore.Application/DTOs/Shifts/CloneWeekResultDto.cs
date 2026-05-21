namespace ToyStore.Application.DTOs.Shifts;

public sealed class CloneWeekResultDto
{
    public int Cloned { get; set; }

    public int Skipped { get; set; }

    public List<string> Reasons { get; set; } = new();
}
