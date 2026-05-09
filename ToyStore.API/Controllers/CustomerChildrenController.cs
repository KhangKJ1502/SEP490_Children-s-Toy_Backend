using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.CustomerChildren;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/customer/children")]
[Authorize(Roles = "Customer")]
public class CustomerChildrenController : ControllerBase
{
    private readonly ICustomerChildService _customerChildService;

    public CustomerChildrenController(ICustomerChildService customerChildService)
    {
        _customerChildService = customerChildService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerChildDto>>> GetMyChildren(CancellationToken cancellationToken = default)
    {
        var result = await _customerChildService.GetMyChildrenAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<CustomerChildDto>> CreateChild(
        [FromBody] CreateChildDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerChildService.CreateChildAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerChildDto>> UpdateChild(
        int id,
        [FromBody] UpdateChildDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerChildService.UpdateChildAsync(id, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteChild(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerChildService.DeleteChildAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
