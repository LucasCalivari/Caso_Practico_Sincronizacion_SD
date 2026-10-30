using Microsoft.AspNetCore.Mvc;

namespace ClockNode.Controllers;

[ApiController]
[Route("api/clock")]
public sealed class ClockController : ControllerBase
{
    private readonly SimulatedClock _clock;
    private readonly IConfiguration _configuration;

    public ClockController(SimulatedClock clock, IConfiguration configuration)
    {
        _clock = clock;
        _configuration = configuration;
    }

    // Consulta inmediata, solo para inspeccionar el estado (no se loguea).
    [HttpGet]
    public IActionResult Get() => Ok(BuildSample());

    // Consulta que usa el coordinador; admite una demora artificial.
    [HttpGet("delayed")]
    public async Task<IActionResult> GetDelayed(CancellationToken ct)
    {
        var delay = Math.Max(0, _configuration.GetValue<int>("RESPONSE_DELAY_MS"));
        if (delay > 0) await Task.Delay(delay, ct);

        var sample = BuildSample();
        AppLog.Write(_clock.Name, _clock.Stamp(),
            $"Consulta de hora respondida (demora={delay} ms, offset={_clock.OffsetMilliseconds:F1} ms)");
        return Ok(sample);
    }

    public record AdjustRequest(double DeltaMs);

    [HttpPost("adjust")]
    public IActionResult Adjust([FromBody] AdjustRequest request)
    {
        if (!double.IsFinite(request.DeltaMs) || Math.Abs(request.DeltaMs) > 3_600_000)
            return BadRequest("Corrección fuera del rango permitido.");

        var newOffset = _clock.Adjust(request.DeltaMs);
        AppLog.Write(_clock.Name, _clock.Stamp(),
            $"Ajuste recibido: {request.DeltaMs:F1} ms (offset {newOffset - request.DeltaMs:F1} -> {newOffset:F1} ms)");
        return Ok(new { node = _clock.Name, newOffsetMs = newOffset });
    }

    private object BuildSample() => new
    {
        node = _clock.Name,
        utcTime = _clock.Now(),
        offsetMs = _clock.OffsetMilliseconds
    };
}