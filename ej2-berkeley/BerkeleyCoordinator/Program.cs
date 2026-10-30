using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

const string ServiceName = "coordinator";

// --- Configuración (variables de entorno, las define docker-compose.yml) ---
var nodes = (Environment.GetEnvironmentVariable("NODES") ??
             "http://node1:8080,http://node2:8080,http://node3:8080")
    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

int intervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("SYNC_INTERVAL_SECONDS"), out var parsedInterval)
    ? Math.Max(1, parsedInterval) : 15;

double ownOffsetMs = double.TryParse(
    Environment.GetEnvironmentVariable("INITIAL_OFFSET_MS"),
    NumberStyles.Float, CultureInfo.InvariantCulture, out var initial)
    ? initial : 0;

double outlierThresholdMs = double.TryParse(
    Environment.GetEnvironmentVariable("OUTLIER_THRESHOLD_MS"),
    NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold)
    ? Math.Max(0, threshold) : 60_000;

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
var stopping = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stopping.Cancel(); };

// --- Utilidades de log: el campo [reloj] es la hora simulada del coordinador ---
string ClockStamp() => DateTimeOffset.UtcNow.AddMilliseconds(ownOffsetMs)
    .UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
void Log(string message) => AppLog.Write(ServiceName, ClockStamp(), message);
string NodeName(string url) => new Uri(url).Host;

Log($"Coordinador Berkeley iniciado. Nodos=[{string.Join(", ", nodes.Select(NodeName))}], " +
    $"intervalo={intervalSeconds} s, umbral={outlierThresholdMs} ms");

// depends_on solo ordena el arranque de los contenedores: damos unos segundos
// para que los nodos terminen de iniciar antes de la primera ronda.
try { await Task.Delay(TimeSpan.FromSeconds(3), stopping.Token); }
catch (OperationCanceledException) { }

int round = 0;
while (!stopping.IsCancellationRequested)
{
    round++;
    Log($"--- Ronda {round} (offset del coordinador = {ownOffsetMs:F1} ms) ---");

    try
    {
        // Referencia común: hora simulada del coordinador al iniciar la ronda.
        var referenceUtc = DateTimeOffset.UtcNow;
        var coordinatorAtReference = referenceUtc.AddMilliseconds(ownOffsetMs);
        var measurements = new List<Measurement>();

        // 1) Consultar la hora de cada nodo y medir el RTT
        foreach (var node in nodes)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var sample = await http.GetFromJsonAsync<ClockSample>(
                    $"{node}/api/clock/delayed", stopping.Token);
                sw.Stop();
                var after = DateTimeOffset.UtcNow;

                if (sample is null) throw new Exception("Respuesta vacía");

                var rtt = sw.Elapsed.TotalMilliseconds;

                // Hora remota estimada al recibir la respuesta (Cristian: + RTT/2)
                var estimatedRemoteAtReceive = sample.UtcTime.AddMilliseconds(rtt / 2.0);
                // Se traslada esa estimación al instante de referencia de la ronda
                var estimatedRemoteAtReference = estimatedRemoteAtReceive - (after - referenceUtc);
                var differenceMs = (estimatedRemoteAtReference - coordinatorAtReference).TotalMilliseconds;

                measurements.Add(new Measurement(node, differenceMs, rtt));
                Log($"{NodeName(node)}: diferencia={differenceMs:F1} ms, RTT={rtt:F1} ms");
            }
            catch (Exception ex) when (!stopping.IsCancellationRequested)
            {
                Log($"{NodeName(node)}: no disponible ({ex.Message})");
            }
        }

        // 2) Descartar desfases extremos
        var accepted = measurements
            .Where(x => Math.Abs(x.DifferenceMs) <= outlierThresholdMs)
            .ToList();
        var rejected = measurements
            .Where(x => Math.Abs(x.DifferenceMs) > outlierThresholdMs);

        foreach (var r in rejected)
            Log($"Descartado por desfase excesivo: {NodeName(r.Node)} " +
                $"({r.DifferenceMs:F1} ms, umbral {outlierThresholdMs} ms)");

        if (accepted.Count == 0)
        {
            Log("Sin participantes válidos; se omite la ronda.");
        }
        else
        {
            // 3) Promedio incluyendo al coordinador (su diferencia es 0)
            double meanMs = accepted.Sum(x => x.DifferenceMs) / (accepted.Count + 1);
            Log($"Promedio de diferencias: {meanMs:F1} ms " +
                $"({accepted.Count} nodos + coordinador)");

            // 4) Enviar a cada nodo su corrección: promedio - su diferencia.
            //    Los nodos descartados no entran en el promedio, pero también
            //    reciben su corrección para alinearse con el resto del grupo.
            foreach (var a in measurements)
            {
                double correctionMs = meanMs - a.DifferenceMs;
                try
                {
                    using var response = await http.PostAsJsonAsync(
                        $"{a.Node}/api/clock/adjust",
                        new { DeltaMs = correctionMs }, stopping.Token);
                    response.EnsureSuccessStatusCode();
                    var nota = accepted.Contains(a) ? "" : " (nodo descartado del promedio)";
                    Log($"Corrección enviada a {NodeName(a.Node)}: {correctionMs:F1} ms{nota}");
                }
                catch (Exception ex) when (!stopping.IsCancellationRequested)
                {
                    Log($"Error al ajustar {NodeName(a.Node)}: {ex.Message}");
                }
            }

            // 5) El coordinador se aplica el promedio a sí mismo
            ownOffsetMs += meanMs;
            Log($"Nuevo offset del coordinador: {ownOffsetMs:F1} ms");
        }
    }
    catch (OperationCanceledException) when (stopping.IsCancellationRequested)
    {
        break;
    }

    try { await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stopping.Token); }
    catch (OperationCanceledException) { break; }
}

Log("Coordinador detenido.");

// Los tipos van después de las instrucciones de nivel superior
public sealed record ClockSample(
    [property: JsonPropertyName("node")] string Node,
    [property: JsonPropertyName("utcTime")] DateTimeOffset UtcTime,
    [property: JsonPropertyName("offsetMs")] double OffsetMs);

public sealed record Measurement(string Node, double DifferenceMs, double RttMs);