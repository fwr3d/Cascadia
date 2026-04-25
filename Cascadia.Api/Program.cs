using Cascadia.Api.Services;

var builder = WebApplication.CreateBuilder(args);

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var origins = new List<string>
    {
        "http://localhost:3000"
    };

    var configuredOrigins = configuration["CORS_ALLOWED_ORIGINS"];
    if (!string.IsNullOrWhiteSpace(configuredOrigins))
    {
        origins.AddRange(configuredOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    var frontendOrigin = configuration["FRONTEND_ORIGIN"];
    if (!string.IsNullOrWhiteSpace(frontendOrigin))
    {
        origins.Add(frontendOrigin.Trim());
    }

    return origins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

bool IsAllowedOrigin(string origin, IReadOnlyCollection<string> allowedOrigins)
{
    if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    foreach (var allowedOrigin in allowedOrigins.Where(value => value.Contains("*", StringComparison.Ordinal)))
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri) ||
            !Uri.TryCreate(allowedOrigin.Replace("*.", "placeholder."), UriKind.Absolute, out var allowedUri))
        {
            continue;
        }

        var wildcardHost = allowedOrigin.Replace($"{allowedUri.Scheme}://*.", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (string.Equals(originUri.Scheme, allowedUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
            originUri.Host.EndsWith(wildcardHost, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    return false;
}

var allowedOrigins = GetAllowedOrigins(builder.Configuration);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => IsAllowedOrigin(origin, allowedOrigins))
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.AddSingleton<InfrastructureService>();
builder.Services.AddSingleton<SimulationService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");
app.MapControllers();

var infra = app.Services.GetRequiredService<InfrastructureService>();
_ = infra.WarmupAsync();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();
