namespace ToyStore.Application.DTOs.Shifts;

public class TransferLoadResultDto
{
    public int TransferredCount { get; set; }

    public int KeptCount { get; set; }

    public List<int> TransferredOrderIds { get; set; } = [];
}
