using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TimeClient;

// --- Configuración desde variables de entorno ---
var clientName = Environment.GetEnvironmentVariable("CLIENT_NAME") ?? "client-x";
var serverUrl = (Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://time-server:8080").TrimEnd('/');

var initialOffsetMs = double.TryParse(
    Environment.GetEnvironmentVariable("INITIAL_OFFSET_MS"),
    NumberStyles.Float,
    CultureInfo.InvariantCulture,
    out var parsedOffset) ? parsedOffset : 0.0;

var intervalSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("SYNC_INTERVAL_SECONDS"),
    out var parsedInterval) ? Math.Max(1, parsedInterval) : 10;

var maxRounds = int.TryParse(
    Environment.GetEnvironmentVariable("MAX_ROUNDS"),
    out var parsedMaxRounds) ? parsedMaxRounds : 0; // 0 = infinito

var timeoutSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("TIMEOUT_SECONDS"),
    out var parsedTimeout) ? Math.Max(1, parsedTimeout) : 4;

var clock = new SimulatedClock(clientName, initialOffsetMs);

using var http = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
};

var stopping = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stopping.Cancel();
};

void Log(string mensaje) => AppLog.Write(clock.Name, clock.Stamp(), mensaje);

Log($"Cliente Cristian iniciado. Servidor destino: {serverUrl}. Offset inicial: {clock.OffsetMilliseconds:+#0.0;-#0.0;0.0} ms");

// Espera breve para asegurar que el servidor de tiempo haya levantado su API
try
{
    await Task.Delay(TimeSpan.FromSeconds(3), stopping.Token);
}
catch (OperationCanceledException)
{
    return;
}

// Simulación del caso de negocio: Registro de operación previa a la sincronización
Log($"[OPERACION PRE-SYNC] OpID=OP-{clock.Name}-01 registrada con hora local desfasada: {clock.Stamp()} (Offset: {clock.OffsetMilliseconds:+#0.0;-#0.0;0.0} ms)");

int round = 0;
while (!stopping.IsCancellationRequested)
{
    round++;
    if (maxRounds > 0 && round > maxRounds)
    {
        Log($"Límite de {maxRounds} rondas alcanzado. Finalizando cliente.");
        break;
    }

    Log($"--- Ronda de sincronización #{round} (Cristian) ---");

    try
    {
        // -------------------------------------------------------------
        // Punto 2: Registrar el instante de envío de la solicitud (T0)
        // -------------------------------------------------------------
        var t0Local = clock.Now();
        var sw = Stopwatch.StartNew();

        Log($"[Punto 2] Instante T0 (envío): {t0Local:yyyy-MM-ddTHH:mm:ss.fffZ}");

        // -------------------------------------------------------------
        // Punto 1: Consultar la hora del servidor de tiempo mediante API REST
        // -------------------------------------------------------------
        Log($"[Punto 1] Consultando {serverUrl}/api/time?client={clientName}...");
        var requestUrl = $"{serverUrl}/api/time?client={Uri.EscapeDataString(clientName)}";
        var response = await http.GetFromJsonAsync<TimeServerResponse>(requestUrl, stopping.Token);

        sw.Stop();

        // -------------------------------------------------------------
        // Punto 3: Registrar el instante de recepción de la respuesta (T1)
        // -------------------------------------------------------------
        var t1Local = clock.Now();
        Log($"[Punto 3] Instante T1 (recepción): {t1Local:yyyy-MM-ddTHH:mm:ss.fffZ}");

        if (response is null)
        {
            throw new InvalidOperationException("La respuesta del servidor de tiempo fue nula.");
        }

        var tServer = response.ServerTime;
        Log($"Hora reportada por el servidor (T_server): {tServer:yyyy-MM-ddTHH:mm:ss.fffZ} (Offset servidor: {response.OffsetMs:+#0.0;-#0.0;0.0} ms)");

        // -------------------------------------------------------------
        // Punto 4: Calcular el tiempo de ida y vuelta (RTT)
        // -------------------------------------------------------------
        var rttMs = (t1Local - t0Local).TotalMilliseconds;
        var rttSwMs = sw.Elapsed.TotalMilliseconds;
        // Se usa la medida de alta resolución del stopwatch o la diferencia t1 - t0
        var finalRttMs = Math.Max(rttMs, rttSwMs);
        var halfRttMs = finalRttMs / 2.0;

        Log($"[Punto 4] RTT calculado: {finalRttMs:F2} ms (Retardo unidireccional estimado RTT/2: {halfRttMs:F2} ms)");

        // -------------------------------------------------------------
        // Punto 5: Estimar la hora actual utilizando el algoritmo de Cristian
        // Hora estimada = T_servidor + (RTT / 2)
        // -------------------------------------------------------------
        var tEstimada = tServer.AddMilliseconds(halfRttMs);
        Log($"[Punto 5] Hora estimada por Cristian [T_server + RTT/2]: {tEstimada:yyyy-MM-ddTHH:mm:ss.fffZ} (Incertidumbre: ±{halfRttMs:F2} ms)");

        // -------------------------------------------------------------
        // Punto 6: Ajustar su reloj lógico de aplicación, sin modificar
        // el reloj del sistema operativo.
        // Diferencia a corregir: Delta = T_estimada - T1_local
        // -------------------------------------------------------------
        var horaAnterior = t1Local;
        var deltaMs = (tEstimada - horaAnterior).TotalMilliseconds;

        var (oldOffset, newOffset) = clock.Adjust(deltaMs);
        Log($"[Punto 6] Reloj lógico ajustado sin tocar el reloj del SO. Corrección aplicada: {deltaMs:+#0.0;-#0.0;0.0} ms. (Offset: {oldOffset:+#0.0;-#0.0;0.0} ms -> {newOffset:+#0.0;-#0.0;0.0} ms)");

        // -------------------------------------------------------------
        // Punto 7: Mostrar la diferencia entre la hora anterior y la hora sincronizada
        // -------------------------------------------------------------
        var horaSincronizada = clock.Now();
        Log($"[Punto 7] RESULTADO SINCRONIZACIÓN:" +
            $"\n\t-> Hora anterior al ajuste (T1):      {horaAnterior:yyyy-MM-ddTHH:mm:ss.fffZ}" +
            $"\n\t-> Hora sincronizada estimada:         {tEstimada:yyyy-MM-ddTHH:mm:ss.fffZ}" +
            $"\n\t-> Diferencia (ajuste realizado):      {deltaMs:+#0.0;-#0.0;0.0} ms" +
            $"\n\t-> Hora local actual post-ajuste:      {horaSincronizada:yyyy-MM-ddTHH:mm:ss.fffZ}");

        // Registro de operación posterior a la sincronización en la ronda 1
        if (round == 1)
        {
            Log($"[OPERACION POST-SYNC] OpID=OP-{clock.Name}-02 registrada con hora sincronizada: {clock.Stamp()} (Consistente con servidor)");
        }
    }
    catch (OperationCanceledException) when (stopping.IsCancellationRequested)
    {
        break;
    }
    catch (Exception ex)
    {
        // Tratamiento ante fallas: Pregunta 3 (¿Qué ocurre si el servidor de tiempo deja de responder?)
        Log($"[ALERTA FALLA] No se pudo sincronizar con {serverUrl}: {ex.Message}. " +
            $"El cliente mantiene su reloj lógico actual ({clock.Stamp()}) y continuará operando de forma autónoma.");
    }

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stopping.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

Log("Cliente Cristian detenido.");

public sealed record TimeServerResponse(
    [property: JsonPropertyName("server")] string Server,
    [property: JsonPropertyName("serverTime")] DateTimeOffset ServerTime,
    [property: JsonPropertyName("offsetMs")] double OffsetMs);
