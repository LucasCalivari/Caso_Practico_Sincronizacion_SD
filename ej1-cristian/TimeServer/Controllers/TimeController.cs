using Microsoft.AspNetCore.Mvc;

namespace TimeServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TimeController : ControllerBase
{
    private readonly SimulatedClock _clock;
    private readonly IConfiguration _configuration;

    public TimeController(SimulatedClock clock, IConfiguration configuration)
    {
        _clock = clock;
        _configuration = configuration;
    }

    /// <summary>
    /// Consulta de hora del servidor (Algoritmo de Cristian).
    /// Admite simular latencia de red mediante ?delayMs= o la variable RESPONSE_DELAY_MS.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTime([FromQuery] int? delayMs, [FromQuery] string? client, CancellationToken ct)
    {
        var configDelay = _configuration.GetValue<int>("RESPONSE_DELAY_MS");
        var actualDelay = Math.Max(0, delayMs ?? configDelay);

        if (actualDelay > 0)
        {
            await Task.Delay(actualDelay, ct);
        }

        var currentTime = _clock.Now();
        var clientName = !string.IsNullOrWhiteSpace(client) ? client : "cliente_desconocido";

        AppLog.Write(_clock.Name, _clock.Stamp(),
            $"Hora despachada a [{clientName}]: {currentTime:yyyy-MM-ddTHH:mm:ss.fffZ} (demora={actualDelay} ms, offset={_clock.OffsetMilliseconds:F1} ms)");

        return Ok(new
        {
            server = _clock.Name,
            serverTime = currentTime,
            offsetMs = _clock.OffsetMilliseconds
        });
    }
}
