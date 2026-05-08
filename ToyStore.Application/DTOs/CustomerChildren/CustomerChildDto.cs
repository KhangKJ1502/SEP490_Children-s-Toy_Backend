namespace ToyStore.Application.DTOs.CustomerChildren;

public class CustomerChildDto
{
    public int ChildId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public DateTime Dob { get; set; }
    public byte? SexId { get; set; }
    public string? SexName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
