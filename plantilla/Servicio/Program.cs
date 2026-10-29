var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

AppLog.Write("servicio", "-", "Servicio iniciado");

app.Run();
