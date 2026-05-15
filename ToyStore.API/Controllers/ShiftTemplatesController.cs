using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Shifts;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/shift-templates")]
public class ShiftTemplatesController : ControllerBase
{
    private readonly IShiftTemplateService _shiftTemplateService;

    public ShiftTemplatesController(IShiftTemplateService shiftTemplateService)
    {
        _shiftTemplateService = shiftTemplateService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ShiftTemplateListDto>>> GetActive(CancellationToken cancellationToken = default)
    {
        var result = await _shiftTemplateService.GetActiveAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ShiftTemplateDto>> Create(
        [FromBody] CreateShiftTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _shiftTemplateService.CreateAsync(dto, cancellationToken);
        return result.ToCreatedResult($"api/shift-templates/{result.Data?.ShiftTemplateId}");
    }

    [HttpPut("{shiftTemplateId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ShiftTemplateDto>> Update(
        [FromRoute] byte shiftTemplateId,
        [FromBody] UpdateShiftTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _shiftTemplateService.UpdateAsync(shiftTemplateId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
