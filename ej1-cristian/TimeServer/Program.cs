using TimeServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SimulatedClock>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

var clock = app.Services.GetRequiredService<SimulatedClock>();
AppLog.Write(clock.Name, clock.Stamp(),
    $"Servidor de tiempo iniciado. Offset inicial: {clock.OffsetMilliseconds:F1} ms (Escuchando en http://+:8080)");

app.Run();
