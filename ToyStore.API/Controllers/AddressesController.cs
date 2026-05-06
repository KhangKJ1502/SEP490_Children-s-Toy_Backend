using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Addresses;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/addresses")]
[Authorize]
public class AddressesController : ControllerBase
{
    private readonly IAddressService _addressService;

    public AddressesController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AddressDto>>> GetMyAddresses(CancellationToken cancellationToken = default)
    {
        var result = await _addressService.GetMyAddressesAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<AddressDto>> CreateMyAddress(
        [FromBody] CreateAddressDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _addressService.CreateMyAddressAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{addressId:int}")]
    public async Task<ActionResult<AddressDto>> UpdateMyAddress(
        int addressId,
        [FromBody] UpdateAddressDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _addressService.UpdateMyAddressAsync(addressId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{addressId:int}")]
    public async Task<ActionResult> DeleteMyAddress(int addressId, CancellationToken cancellationToken = default)
    {
        var result = await _addressService.DeleteMyAddressAsync(addressId, cancellationToken);
        return result.ToActionResult();
    }
}
