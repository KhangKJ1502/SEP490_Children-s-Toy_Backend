namespace ToyStore.Application.DTOs.CustomerChildren;

public class CreateChildDto
{
    public string FullName { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public DateTime Dob { get; set; }
    public byte? SexId { get; set; }
}
