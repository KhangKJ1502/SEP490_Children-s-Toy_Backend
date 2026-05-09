using Microsoft.AspNetCore.Http;

namespace ToyStore.Application.DTOs.Reviews;

public class UpdateReviewProductDto
{
    public byte? Rating { get; set; }
    
    public string? Comment { get; set; }
    
    public List<IFormFile>? Images { get; set; }
}
