using Cascadia.Api.Models;
using Cascadia.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cascadia.Api.Controllers;

[ApiController]
[Route("api/simulate")]
public sealed class SimulateController : ControllerBase
{
    private readonly SimulationService _simulationService;

    public SimulateController(SimulationService simulationService)
    {
        _simulationService = simulationService;
    }

    [HttpPost]
    public async Task<ActionResult<SimulationResponseDto>> SimulateAsync(
        [FromBody] SimulationRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _simulationService.SimulateAsync(request, cancellationToken);
        return Ok(response);
    }
}
