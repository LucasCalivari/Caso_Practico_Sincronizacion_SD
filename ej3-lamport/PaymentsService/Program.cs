var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<LamportClock>();
var app = builder.Build();
app.MapControllers();
AppLog.Write("payments-service", "-", "Servicio iniciado");
app.Run();
