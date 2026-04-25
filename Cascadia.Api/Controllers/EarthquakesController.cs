using System.Text.Json;
using Cascadia.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Cascadia.Api.Controllers;

[ApiController]
[Route("api/earthquakes")]
public sealed class EarthquakesController : ControllerBase
{
    private const string CacheKey = "usgs-earthquakes-v1";
    private const string UsgsUrl =
        "https://earthquake.usgs.gov/fdsnws/event/1/query?format=geojson&minmagnitude=4.5&minlatitude=18&maxlatitude=72&minlongitude=-170&maxlongitude=-60&limit=20&orderby=time";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EarthquakesController> _logger;

    public EarthquakesController(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<EarthquakesController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EarthquakeDto>>> GetEarthquakes(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out List<EarthquakeDto>? cached) && cached is not null)
        {
            return Ok(cached);
        }

        var earthquakes = await FetchEarthquakesAsync(cancellationToken);
        _cache.Set(CacheKey, earthquakes, TimeSpan.FromMinutes(5));
        return Ok(earthquakes);
    }

    private async Task<List<EarthquakeDto>> FetchEarthquakesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(UsgsUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("features", out var features) ||
                features.ValueKind != JsonValueKind.Array)
            {
                return GetFallbackEarthquakes();
            }

            var earthquakes = new List<EarthquakeDto>();

            foreach (var feature in features.EnumerateArray())
            {
                var id = feature.TryGetProperty("id", out var idProperty) ? idProperty.GetString() : null;
                var properties = feature.TryGetProperty("properties", out var propsProperty) ? propsProperty : default;
                var geometry = feature.TryGetProperty("geometry", out var geometryProperty) ? geometryProperty : default;
                var coordinates = geometry.ValueKind == JsonValueKind.Object &&
                                  geometry.TryGetProperty("coordinates", out var coordinatesProperty)
                    ? coordinatesProperty
                    : default;

                if (string.IsNullOrWhiteSpace(id) ||
                    properties.ValueKind != JsonValueKind.Object ||
                    coordinates.ValueKind != JsonValueKind.Array ||
                    coordinates.GetArrayLength() < 3)
                {
                    continue;
                }

                var lon = coordinates[0].GetDouble();
                var lat = coordinates[1].GetDouble();
                var depthKm = coordinates[2].GetDouble();
                var magnitude = properties.TryGetProperty("mag", out var magProperty) && magProperty.TryGetDouble(out var mag)
                    ? mag
                    : 0;

                var timeMs = properties.TryGetProperty("time", out var timeProperty) && timeProperty.TryGetInt64(out var time)
                    ? time
                    : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                earthquakes.Add(new EarthquakeDto
                {
                    Id = id,
                    Title = properties.TryGetProperty("title", out var titleProperty) ? titleProperty.GetString() ?? string.Empty : string.Empty,
                    Place = properties.TryGetProperty("place", out var placeProperty) ? placeProperty.GetString() ?? string.Empty : string.Empty,
                    Magnitude = Math.Round(magnitude, 1),
                    DepthKm = Math.Round(depthKm, 1),
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(timeMs),
                    Lat = Math.Round(lat, 4),
                    Lon = Math.Round(lon, 4),
                    Url = properties.TryGetProperty("url", out var urlProperty) ? urlProperty.GetString() ?? string.Empty : string.Empty
                });
            }

            return earthquakes.Count > 0
                ? earthquakes.OrderByDescending(quake => quake.Time).ToList()
                : GetFallbackEarthquakes();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "USGS fetch failed. Returning built-in earthquake fallback list.");
            return GetFallbackEarthquakes();
        }
    }

    private static List<EarthquakeDto> GetFallbackEarthquakes()
    {
        return
        [
            new()
            {
                Id = "ak1964goodfriday",
                Title = "M 9.2 - 1964 Great Alaska Earthquake",
                Place = "Prince William Sound, Alaska",
                Magnitude = 9.2,
                DepthKm = 25,
                Time = new DateTimeOffset(1964, 3, 28, 3, 36, 0, TimeSpan.Zero),
                Lat = 61.02,
                Lon = -147.65,
                Url = "https://earthquake.usgs.gov/"
            },
            new()
            {
                Id = "alaska20230716",
                Title = "M 7.2 - Alaska Peninsula",
                Place = "98 km S of Sand Point, Alaska",
                Magnitude = 7.2,
                DepthKm = 21.4,
                Time = new DateTimeOffset(2023, 7, 16, 6, 48, 21, TimeSpan.Zero),
                Lat = 54.62,
                Lon = -160.56,
                Url = "https://earthquake.usgs.gov/"
            },
            new()
            {
                Id = "ncal20221220",
                Title = "M 6.4 - Northern California",
                Place = "15 km WSW of Ferndale, California",
                Magnitude = 6.4,
                DepthKm = 17.9,
                Time = new DateTimeOffset(2022, 12, 20, 10, 34, 24, TimeSpan.Zero),
                Lat = 40.53,
                Lon = -124.42,
                Url = "https://earthquake.usgs.gov/"
            },
            new()
            {
                Id = "ridgecrest20190706",
                Title = "M 7.1 - Ridgecrest, California",
                Place = "17 km NNE of Ridgecrest, California",
                Magnitude = 7.1,
                DepthKm = 8,
                Time = new DateTimeOffset(2019, 7, 6, 3, 19, 53, TimeSpan.Zero),
                Lat = 35.77,
                Lon = -117.6,
                Url = "https://earthquake.usgs.gov/"
            }
        ];
    }
}
