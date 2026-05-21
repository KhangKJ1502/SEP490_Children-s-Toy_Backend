using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Shifts;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/work-schedules")]
public class WorkSchedulesController : ControllerBase
{
    private readonly IWorkScheduleService _workScheduleService;

    public WorkSchedulesController(IWorkScheduleService workScheduleService)
    {
        _workScheduleService = workScheduleService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WorkScheduleDto>> Create(
        [FromBody] CreateWorkScheduleDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _workScheduleService.CreateAsync(dto, cancellationToken);
        return result.ToCreatedResult($"api/work-schedules/{result.Data?.ScheduleId}");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<WorkScheduleListDto>>> GetList(
        [FromQuery] DateTime? workDate,
        [FromQuery] string? status,
        [FromQuery] byte? roleId,
        CancellationToken cancellationToken = default)
    {
        var query = new WorkScheduleQueryDto
        {
            WorkDate = workDate,
            Status = status,
            RoleId = roleId
        };

        var result = await _workScheduleService.GetListAsync(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{scheduleId:int}/absent")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MarkAbsentResultDto>> MarkAbsent(
        [FromRoute] int scheduleId,
        CancellationToken cancellationToken = default)
    {
        var result = await _workScheduleService.MarkAbsentAsync(scheduleId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("clone-week")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CloneWeekResultDto>> CloneWeek(
        [FromQuery] DateTime sourceMonday,
        [FromQuery] DateTime targetMonday,
        CancellationToken cancellationToken = default)
    {
        var result = await _workScheduleService.CloneWeekAsync(sourceMonday, targetMonday, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{scheduleId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UpdateWorkScheduleResultDto>> Update(
        [FromRoute] int scheduleId,
        [FromBody] UpdateWorkScheduleDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _workScheduleService.UpdateAsync(scheduleId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{scheduleId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(
        [FromRoute] int scheduleId,
        CancellationToken cancellationToken = default)
    {
        var result = await _workScheduleService.DeleteAsync(scheduleId, cancellationToken);
        return result.ToActionResult();
    }
}
