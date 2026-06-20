using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Templates;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Staff,Merchandise")]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(ITemplateService templateService, ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TemplateListDto>>> GetTemplates(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? usageScope = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _templateService.GetTemplatesAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            isActive,
            usageScope,
            startDate,
            endDate,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<TemplateListDto>> CreateTemplate(
        [FromBody] CreateTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _templateService.CreateTemplateAsync(dto, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Created template {TemplateId}", result.Data!.TemplateId);
        }

        return result.ToCreatedResult($"api/templates/{result.Data?.TemplateId}");
    }

    [HttpPut("{templateId:int}")]
    public async Task<ActionResult> SaveTemplate(
        [FromRoute] short templateId,
        [FromBody] UpdateTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.IsDeleted && !User.IsInRole("Admin"))
            return Forbid();

        var result = await _templateService.SaveTemplateAsync(templateId, dto, cancellationToken);

        if (!result.IsSuccess)
            return result.ToActionResult().Result ?? new StatusCodeResult(500);

        if (dto.IsDeleted)
            return NoContent();

        return Ok(result.Data);
    }
}
