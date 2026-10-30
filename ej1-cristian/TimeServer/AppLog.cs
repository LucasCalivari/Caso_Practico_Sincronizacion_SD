/// <summary>
/// Formato de log común a los tres ejercicios:
/// [hora real] [servicio] [reloj] mensaje
/// - hora real: UTC del host, ISO 8601 con milisegundos.
/// - servicio: nombre del contenedor (p. ej. client-a, time-server, node1).
/// - reloj: valor del reloj lógico/simulado del servicio ("-" si no aplica).
/// </summary>
public static class AppLog
{
    public static void Write(string servicio, string reloj, string mensaje)
    {
        var horaReal = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        Console.WriteLine($"[{horaReal}] [{servicio}] [{reloj}] {mensaje}");
    }
}
