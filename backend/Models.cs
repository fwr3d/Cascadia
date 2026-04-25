namespace CascadiaApi.Models;

public record SimulateRequest(
    double EpicenterLat,
    double EpicenterLon,
    double Magnitude,
    double DepthKm
);

public record WaveRing(double RadiusKm, double EtaMinutes, double WaveHeightM);

public record County(string Name, string State, int Population, double DistanceKm);

public record InfraItem(
    string Name,
    string Type,
    double Lat,
    double Lon,
    double DistanceKm,
    int HitAtRingIndex,
    double GridCoverageRadiusKm
);

public record SimulateResponse(
    double Magnitude,
    double EpicenterLat,
    double EpicenterLon,
    double DepthKm,
    double EnergyJoules,
    List<WaveRing> Rings,
    List<County> CountiesAtRisk,
    List<InfraItem> InfrastructureAtRisk,
    long TotalPopulationAtRisk,
    double MaxWaveHeightM
);
