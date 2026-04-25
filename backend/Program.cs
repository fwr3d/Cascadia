using CascadiaApi.Models;
using CascadiaApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SimulationService>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

var app = builder.Build();
app.UseCors();

app.MapPost("/api/simulate", (SimulateRequest req, SimulationService svc) => svc.Run(req));

app.Run();
