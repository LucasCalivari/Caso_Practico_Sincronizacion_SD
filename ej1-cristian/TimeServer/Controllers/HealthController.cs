using Microsoft.AspNetCore.Mvc;

namespace TimeServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly SimulatedClock _clock;

    public HealthController(SimulatedClock clock)
    {
        _clock = clock;
    }

    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        server = _clock.Name,
        serverTime = _clock.Now(),
        offsetMs = _clock.OffsetMilliseconds
    });
}
