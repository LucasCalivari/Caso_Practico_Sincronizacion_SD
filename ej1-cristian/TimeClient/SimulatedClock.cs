using System.Globalization;

namespace TimeClient;

/// <summary>
/// Reloj lógico de aplicación que simula un reloj físico local con desfase,
/// permitiendo su ajuste mediante el algoritmo de Cristian sin modificar
/// el reloj del sistema operativo (Punto 6).
/// </summary>
public sealed class SimulatedClock
{
    private readonly object _sync = new();
    private double _offsetMs;

    public string Name { get; }

    public SimulatedClock(string name, double initialOffsetMs)
    {
        Name = name;
        _offsetMs = initialOffsetMs;
    }

    /// <summary>Hora física local de la aplicación: UTC real + offset.</summary>
    public DateTimeOffset Now()
    {
        lock (_sync)
            return DateTimeOffset.UtcNow.AddMilliseconds(_offsetMs);
    }

    /// <summary>Hora local formateada en ISO 8601 UTC para el campo [reloj] del log.</summary>
    public string Stamp() =>
        Now().UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    public double OffsetMilliseconds
    {
        get { lock (_sync) return _offsetMs; }
    }

    /// <summary>
    /// Ajusta el reloj sumando una corrección deltaMs (en milisegundos).
    /// Devuelve (offsetAnterior, offsetNuevo).
    /// </summary>
    public (double OldOffset, double NewOffset) Adjust(double deltaMs)
    {
        lock (_sync)
        {
            var old = _offsetMs;
            _offsetMs += deltaMs;
            return (old, _offsetMs);
        }
    }
}
