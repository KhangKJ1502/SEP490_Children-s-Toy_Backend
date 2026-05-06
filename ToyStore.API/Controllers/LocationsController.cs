using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Addresses;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly IAddressService _addressService;

    public LocationsController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet("provinces")]
    public async Task<ActionResult<List<ProvinceOptionDto>>> GetProvinces(CancellationToken cancellationToken = default)
    {
        var result = await _addressService.GetProvincesAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("districts")]
    public async Task<ActionResult<List<DistrictOptionDto>>> GetDistricts(
        [FromQuery] int provinceId,
        CancellationToken cancellationToken = default)
    {
        var result = await _addressService.GetDistrictsAsync(provinceId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("wards")]
    public async Task<ActionResult<List<WardOptionDto>>> GetWards(
        [FromQuery] int districtId,
        CancellationToken cancellationToken = default)
    {
        var result = await _addressService.GetWardsAsync(districtId, cancellationToken);
        return result.ToActionResult();
    }
}
