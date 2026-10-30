using System.Globalization;

namespace TimeServer;

public sealed class SimulatedClock
{
    private readonly object _sync = new();
    private double _offsetMs;

    public string Name { get; }

    public SimulatedClock(IConfiguration config)
    {
        Name = config["SERVER_NAME"] ?? "time-server";

        _offsetMs = double.TryParse(
            config["INITIAL_OFFSET_MS"],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value) ? value : 2000.0; // Caso práctico: 10:00:02 (+2000 ms respecto a 10:00:00)
    }

    /// <summary>Hora física simulada: UTC real + offset.</summary>
    public DateTimeOffset Now()
    {
        lock (_sync)
            return DateTimeOffset.UtcNow.AddMilliseconds(_offsetMs);
    }

    /// <summary>Hora simulada en formato ISO 8601 UTC para el campo [reloj] del log.</summary>
    public string Stamp() =>
        Now().UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    public double OffsetMilliseconds
    {
        get { lock (_sync) return _offsetMs; }
    }
}
