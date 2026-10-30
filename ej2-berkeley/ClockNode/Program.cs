using ClockNode;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<SimulatedClock>();

var app = builder.Build();
app.MapControllers();

var clock = app.Services.GetRequiredService<SimulatedClock>();
AppLog.Write(clock.Name, clock.Stamp(),
    $"Nodo iniciado (offset inicial = {clock.OffsetMilliseconds} ms)");

app.Run();