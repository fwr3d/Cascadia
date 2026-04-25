using CascadiaApi.Models;

namespace CascadiaApi.Services;

public class SimulationService
{
    private const double G = 9.81;
    private const double EarthRadiusKm = 6371;

    // Ring radii in km — frontend maps these to concentric circles
    private static readonly double[] RingRadii = [150, 450, 1000, 2000, 3500];

    public SimulateResponse Run(SimulateRequest req)
    {
        double energyJoules = Math.Pow(10, 1.5 * req.Magnitude + 4.8);

        // Approximate average ocean depth for wave speed (km → m)
        double oceanDepthM = Math.Max(1000, 4000 - req.DepthKm * 10);
        double waveSpeedMs = Math.Sqrt(G * oceanDepthM);       // m/s
        double waveSpeedKmMin = waveSpeedMs * 60.0 / 1000.0;  // km/min

        double h0 = 0.5 * Math.Pow(10, 0.5 * req.Magnitude - 3.5);
        double r0 = RingRadii[0];

        var rings = RingRadii.Select(r => new WaveRing(
            RadiusKm: r,
            EtaMinutes: Math.Round(r / waveSpeedKmMin, 1),
            WaveHeightM: Math.Round(h0 * Math.Sqrt(r0 / r), 2)
        )).ToList();

        // TODO: replace with real county + infra DB lookup by distance
        var counties = new List<County>();
        var infra = new List<InfraItem>();

        long totalPop = counties.Sum(c => (long)c.Population);
        double maxWave = rings.Max(r => r.WaveHeightM);

        return new SimulateResponse(
            Magnitude: req.Magnitude,
            EpicenterLat: req.EpicenterLat,
            EpicenterLon: req.EpicenterLon,
            DepthKm: req.DepthKm,
            EnergyJoules: energyJoules,
            Rings: rings,
            CountiesAtRisk: counties,
            InfrastructureAtRisk: infra,
            TotalPopulationAtRisk: totalPop,
            MaxWaveHeightM: maxWave
        );
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
