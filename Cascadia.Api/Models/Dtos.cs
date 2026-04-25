using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cascadia.Api.Models;

public sealed class SimulationRequestDto
{
    [JsonPropertyName("epicenterLat")]
    [Range(-90, 90)]
    public double EpicenterLat { get; set; }

    [JsonPropertyName("epicenterLon")]
    [Range(-180, 180)]
    public double EpicenterLon { get; set; }

    [JsonPropertyName("magnitude")]
    [Range(0.1, 10)]
    public double Magnitude { get; set; }

    [JsonPropertyName("depthKm")]
    [Range(0.1, 11)]
    public double DepthKm { get; set; }
}

public sealed class SimulationResponseDto
{
    [JsonPropertyName("waveSpeedKmS")]
    public double WaveSpeedKmS { get; set; }

    [JsonPropertyName("energyJoules")]
    public string EnergyJoules { get; set; } = string.Empty;

    [JsonPropertyName("etaNearestCoastMin")]
    public double EtaNearestCoastMin { get; set; }

    [JsonPropertyName("estimatedRunupM")]
    public double EstimatedRunupM { get; set; }

    [JsonPropertyName("affectedPopulation")]
    public int AffectedPopulation { get; set; }

    [JsonPropertyName("rings")]
    public List<WaveRingDto> Rings { get; set; } = [];

    [JsonPropertyName("infrastructureAtRisk")]
    public List<InfrastructureRiskDto> InfrastructureAtRisk { get; set; } = [];
}

public sealed class WaveRingDto
{
    [JsonPropertyName("radiusKm")]
    public double RadiusKm { get; set; }

    [JsonPropertyName("etaMinutes")]
    public int EtaMinutes { get; set; }

    [JsonPropertyName("affectedCounties")]
    public List<AffectedCountyDto> AffectedCounties { get; set; } = [];
}

public sealed class AffectedCountyDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("pop")]
    public int Pop { get; set; }

    [JsonPropertyName("fips")]
    public string Fips { get; set; } = string.Empty;
}

public sealed class InfrastructureRiskDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("distanceKm")]
    public double DistanceKm { get; set; }

    [JsonPropertyName("gridCoverageRadiusKm")]
    public double GridCoverageRadiusKm { get; set; }

    [JsonPropertyName("hitAtRingIndex")]
    public int HitAtRingIndex { get; set; }
}

public sealed class InfrastructureItemDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("distanceKm")]
    public double DistanceKm { get; set; }

    [JsonPropertyName("gridCoverageRadiusKm")]
    public double GridCoverageRadiusKm { get; set; }

    [JsonPropertyName("capacityMw")]
    public double? CapacityMw { get; set; }

    [JsonPropertyName("beds")]
    public int? Beds { get; set; }
}

public sealed class InfrastructureSearchResponseDto
{
    [JsonPropertyName("centerLat")]
    public double CenterLat { get; set; }

    [JsonPropertyName("centerLon")]
    public double CenterLon { get; set; }

    [JsonPropertyName("radiusKm")]
    public double RadiusKm { get; set; }

    [JsonPropertyName("hospitals")]
    public List<InfrastructureItemDto> Hospitals { get; set; } = [];

    [JsonPropertyName("powerPlants")]
    public List<InfrastructureItemDto> PowerPlants { get; set; } = [];

    [JsonPropertyName("ports")]
    public List<InfrastructureItemDto> Ports { get; set; } = [];
}

public sealed class EarthquakeDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("place")]
    public string Place { get; set; } = string.Empty;

    [JsonPropertyName("magnitude")]
    public double Magnitude { get; set; }

    [JsonPropertyName("depthKm")]
    public double DepthKm { get; set; }

    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
