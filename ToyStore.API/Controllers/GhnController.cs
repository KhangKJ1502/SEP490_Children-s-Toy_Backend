using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// GHN master data — tỉnh/huyện/xã dùng cho form địa chỉ checkout.
/// </summary>
[ApiController]
[Route("api/ghn")]
[Authorize]
public class GhnController : ControllerBase
{
    private readonly IGhnClient _ghnClient;

    public GhnController(IGhnClient ghnClient)
    {
        _ghnClient = ghnClient;
    }

    /// <summary>Danh sách tỉnh/thành.</summary>
    [HttpGet("provinces")]
    public async Task<ActionResult<List<GhnProvinceDto>>> GetProvinces(CancellationToken ct)
    {
        var result = await _ghnClient.GetProvincesAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>Danh sách quận/huyện theo tỉnh.</summary>
    [HttpGet("districts")]
    public async Task<ActionResult<List<GhnDistrictDto>>> GetDistricts(
        [FromQuery] int provinceId,
        CancellationToken ct)
    {
        var result = await _ghnClient.GetDistrictsAsync(provinceId, ct);
        return result.ToActionResult();
    }

    /// <summary>Danh sách phường/xã theo quận.</summary>
    [HttpGet("wards")]
    public async Task<ActionResult<List<GhnWardDto>>> GetWards(
        [FromQuery] int districtId,
        CancellationToken ct)
    {
        var result = await _ghnClient.GetWardsAsync(districtId, ct);
        return result.ToActionResult();
    }
}
