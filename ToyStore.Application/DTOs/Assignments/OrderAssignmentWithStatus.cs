using ToyStore.Domain.Entities;

namespace ToyStore.Application.DTOs.Assignments;

public class OrderAssignmentWithStatus
{
    public OrderAssignment Assignment { get; set; } = null!;

    public string StatusName { get; set; } = string.Empty;
}
