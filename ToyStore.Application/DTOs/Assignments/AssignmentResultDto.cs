namespace ToyStore.Application.DTOs.Assignments;

public class AssignmentResultDto
{
    public string Result { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public int? StaffAccountId { get; set; }

    public int? MerchAccountId { get; set; }
}
