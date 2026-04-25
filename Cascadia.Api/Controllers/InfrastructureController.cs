using Cascadia.Api.Models;
using Cascadia.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cascadia.Api.Controllers;

[ApiController]
[Route("api/infrastructure")]
public sealed class InfrastructureController : ControllerBase
{
    private readonly InfrastructureService _infrastructureService;

    public InfrastructureController(InfrastructureService infrastructureService)
    {
        _infrastructureService = infrastructureService;
    }

    [HttpGet]
    public ActionResult<InfrastructureSearchResponseDto> GetInfrastructure(
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] double radiusKm)
    {
        if (lat is < -90 or > 90 || lon is < -180 or > 180 || radiusKm <= 0)
        {
            return BadRequest(new { error = "Provide valid lat, lon, and radiusKm query parameters." });
        }

        var items = _infrastructureService.GetInfrastructureWithinRadius(lat, lon, radiusKm);
        var response = new InfrastructureSearchResponseDto
        {
            CenterLat = lat,
            CenterLon = lon,
            RadiusKm = radiusKm,
            Hospitals = items.Where(item => item.Type == "hospital").ToList(),
            PowerPlants = items.Where(item => item.Type == "powerPlant").ToList(),
            Ports = items.Where(item => item.Type == "port").ToList()
        };

        return Ok(response);
    }
}
