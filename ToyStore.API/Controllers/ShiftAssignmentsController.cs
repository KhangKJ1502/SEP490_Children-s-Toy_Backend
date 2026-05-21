using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Assignments;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api")]
public class ShiftAssignmentsController : ControllerBase
{
    private readonly IShiftAssignmentService _assignmentService;

    public ShiftAssignmentsController(IShiftAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet("order-queue")]
    [Authorize(Policy = "Orders.Admin")]
    public async Task<ActionResult<List<OrderQueueItemDto>>> GetQueue(CancellationToken cancellationToken = default)
    {
        var result = await _assignmentService.GetQueueAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("order-queue/{queueId:int}/assign")]
    [Authorize(Policy = "Orders.Admin")]
    public async Task<ActionResult> AssignQueue(
        [FromRoute] int queueId,
        [FromBody] AssignQueueOrderRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignmentService.AssignQueueAsync(queueId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("orders/{orderId:int}/reassign")]
    [Authorize(Policy = "Orders.Admin")]
    public async Task<ActionResult> ReassignOrder(
        [FromRoute] int orderId,
        [FromBody] ReassignOrderRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignmentService.ReassignOrderAsync(orderId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("staff-shift-capacity/{scheduleId:int}/max-load")]
    [Authorize(Policy = "Orders.Admin")]
    public async Task<ActionResult> UpdateMaxLoad(
        [FromRoute] int scheduleId,
        [FromBody] UpdateShiftCapacityDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignmentService.UpdateMaxLoadAsync(scheduleId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
