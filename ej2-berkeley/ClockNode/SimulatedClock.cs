using System.Globalization;

namespace ClockNode;

public sealed class SimulatedClock
{
    private readonly object _sync = new();
    private double _offsetMs;

    public string Name { get; }

    public SimulatedClock(IConfiguration config)
    {
        Name = config["NODE_NAME"] ?? Environment.MachineName;

        _offsetMs = double.TryParse(
            config["INITIAL_OFFSET_MS"],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value) ? value : 0;
    }

    /// <summary>Hora simulada: UTC real + offset.</summary>
    public DateTimeOffset Now()
    {
        lock (_sync)
            return DateTimeOffset.UtcNow.AddMilliseconds(_offsetMs);
    }

    /// <summary>Hora simulada en formato ISO 8601 para el campo [reloj] del log.</summary>
    public string Stamp() =>
        Now().UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    public double OffsetMilliseconds
    {
        get { lock (_sync) return _offsetMs; }
    }

    /// <summary>Suma una corrección al offset y devuelve el offset nuevo.</summary>
    public double Adjust(double deltaMs)
    {
        lock (_sync)
        {
            _offsetMs += deltaMs;
            return _offsetMs;
        }
    }
}