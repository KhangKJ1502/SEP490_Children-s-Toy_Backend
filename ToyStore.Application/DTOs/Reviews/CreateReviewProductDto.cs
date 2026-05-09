using Microsoft.AspNetCore.Http;

namespace ToyStore.Application.DTOs.Reviews;

public class CreateReviewProductDto
{
    public int OrderId { get; set; }
    
    public int ProductId { get; set; }
    
    public byte Rating { get; set; }
    
    public string? Comment { get; set; }
    
    public List<IFormFile>? Images { get; set; }
}
