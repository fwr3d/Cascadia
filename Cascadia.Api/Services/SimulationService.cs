using System.Globalization;
using Cascadia.Api.Models;

namespace Cascadia.Api.Services;

public sealed class SimulationService
{
    // Ring times in minutes — local coast focus, capped at 30 min
    private static readonly int[] RingMinutes = [0, 5, 10, 20, 30];

    // Mean Pacific slope for inundation: ~0.5% (0.005) — flat coastal plains WA/OR/CA
    private const double CoastalSlopeFraction = 0.005;

    // Pacific Coast reference points: lat, lon, name
    // Defines land/ocean boundary from SE Alaska to Baja + Hawaii
    private static readonly (double Lat, double Lon, string Name)[] PacificCoastPoints =
    [
        (54.8,  -131.5, "Ketchikan, AK"),
        (56.5,  -132.9, "Petersburg, AK"),
        (57.1,  -135.3, "Sitka, AK"),
        (58.3,  -136.9, "Glacier Bay, AK"),
        (59.5,  -139.7, "Yakutat, AK"),
        (60.0,  -141.7, "Malaspina, AK"),
        (59.5,  -146.0, "Kayak Island, AK"),
        (59.3,  -151.4, "Homer, AK"),
        (57.8,  -152.4, "Kodiak, AK"),
        (56.3,  -158.5, "Chignik, AK"),
        (55.3,  -162.3, "Cold Bay, AK"),
        (54.5,  -164.8, "Unimak Pass, AK"),
        (53.9,  -166.5, "Dutch Harbor, AK"),
        (54.3,  -130.3, "Prince Rupert, BC"),
        (50.7,  -127.4, "Port Hardy, BC"),
        (49.2,  -126.0, "Tofino, BC"),
        (48.4,  -123.4, "Victoria, BC"),
        (48.4,  -124.7, "Cape Flattery, WA"),
        (47.6,  -124.4, "Kalaloch, WA"),
        (47.0,  -124.1, "Westport, WA"),
        (46.3,  -124.1, "Cape Disappointment, WA"),
        (45.9,  -123.9, "Cannon Beach, OR"),
        (45.3,  -124.0, "Cape Lookout, OR"),
        (44.2,  -124.1, "Newport, OR"),
        (43.4,  -124.4, "Coos Bay, OR"),
        (42.1,  -124.3, "Brookings, OR"),
        (41.8,  -124.2, "Crescent City, CA"),
        (41.1,  -124.1, "Trinidad, CA"),
        (40.4,  -124.4, "Cape Mendocino, CA"),
        (39.4,  -123.8, "Fort Bragg, CA"),
        (38.4,  -123.3, "Bodega Bay, CA"),
        (38.0,  -123.0, "Point Reyes, CA"),
        (37.8,  -122.5, "San Francisco, CA"),
        (37.2,  -122.4, "Half Moon Bay, CA"),
        (36.9,  -122.0, "Santa Cruz, CA"),
        (36.6,  -121.9, "Monterey, CA"),
        (35.4,  -120.9, "San Luis Obispo, CA"),
        (34.4,  -120.5, "Point Conception, CA"),
        (34.4,  -119.7, "Ventura, CA"),
        (34.0,  -118.5, "Santa Monica, CA"),
        (33.7,  -118.3, "Long Beach, CA"),
        (33.5,  -117.8, "Laguna Beach, CA"),
        (33.2,  -117.4, "Oceanside, CA"),
        (32.7,  -117.2, "San Diego, CA"),
        (32.5,  -117.1, "Tijuana, MX"),
        (31.9,  -116.7, "Ensenada, MX"),
        (29.0,  -115.2, "Punta Eugenia, MX"),
        (27.0,  -114.0, "Guerrero Negro, MX"),
        (24.1,  -110.3, "La Paz, MX"),
        (22.9,  -109.9, "Cabo San Lucas, MX"),
        (22.2,  -159.5, "Kauai, HI"),
        (21.3,  -157.9, "Honolulu, HI"),
        (20.9,  -156.4, "Maui, HI"),
        (19.0,  -155.7, "Big Island, HI"),
    ];

    private readonly InfrastructureService _infrastructureService;

    public SimulationService(InfrastructureService infrastructureService)
    {
        _infrastructureService = infrastructureService;
    }

    public async Task<SimulationResponseDto> SimulateAsync(
        SimulationRequestDto request,
        CancellationToken cancellationToken)
    {
        // c = √(g·h) using mean Pacific ocean depth (not fault depth)
        // 4000m depth → ~0.198 km/s ≈ 713 km/h, consistent with USGS observations
        const double meanOceanDepthM = 4000d;
        var waveSpeedKmS = Math.Sqrt(9.81d * meanOceanDepthM) / 1000d;

        var energyJoules = Math.Pow(10d, 4.8d + (1.5d * request.Magnitude))
            .ToString("0.00e+00", CultureInfo.InvariantCulture);

        var estimatedRunupM = EstimateRunupMeters(request.Magnitude);

        var ringRadiiKm = RingMinutes
            .Select(t => waveSpeedKmS * t * 60d)
            .ToList();

        // Coastal inundation — computed before rings so counties can be filtered correctly
        var coastalInundation = ComputeCoastalInundation(
            request.EpicenterLat, request.EpicenterLon,
            estimatedRunupM, ringRadiiKm);

        // Rings: only count counties within a coastal inundation zone
        var ringSnapshots = new List<WaveRingDto>(RingMinutes.Length);
        var inundationAtRing = coastalInundation
            .GroupBy(z => z.HitAtRingIndex)
            .ToDictionary(g => g.Key, g => g.ToList());

        for (var i = 0; i < RingMinutes.Length; i++)
        {
            var radiusKm = ringRadiiKm[i];
            // Counties affected = those within an inundation zone hit at or before this ring
            var inundationZonesSoFar = coastalInundation
                .Where(z => z.HitAtRingIndex <= i)
                .ToList();

            var affectedCounties = await _infrastructureService
                .GetCountiesInInundationZonesAsync(inundationZonesSoFar, cancellationToken);

            ringSnapshots.Add(new WaveRingDto
            {
                RadiusKm   = Math.Round(radiusKm, 2),
                EtaMinutes = RingMinutes[i],
                AffectedCounties = affectedCounties.ToList()
            });
        }

        // Total affected population = sum of unique counties in the final ring snapshot
        // (avoids double-counting counties near multiple coast reference points)
        var affectedPopulation = ringSnapshots.LastOrDefault()?.AffectedCounties.Sum(c => c.Pop) ?? 0;

        var nearestCoastKm = coastalInundation.Count > 0
            ? coastalInundation.Min(z => z.DistanceFromEpicenterKm)
            : (double?)null;

        var etaNearestCoastMin = nearestCoastKm is null
            ? 0
            : Math.Round(nearestCoastKm.Value / (waveSpeedKmS * 60d), 2);

        return new SimulationResponseDto
        {
            WaveSpeedKmS       = Math.Round(waveSpeedKmS, 3),
            EnergyJoules       = energyJoules,
            EtaNearestCoastMin = etaNearestCoastMin,
            EstimatedRunupM    = Math.Round(estimatedRunupM, 2),
            AffectedPopulation = affectedPopulation,
            Rings              = ringSnapshots,
            InfrastructureAtRisk = [],
            CoastalInundation  = coastalInundation
        };
    }

    private List<CoastalInundationDto> ComputeCoastalInundation(
        double epicenterLat, double epicenterLon,
        double estimatedRunupM,
        IReadOnlyList<double> ringRadiiKm)
    {
        var maxRadius = ringRadiiKm.Count > 0 ? ringRadiiKm.Max() : 0d;
        // Reference: runup near the first non-zero ring
        var firstRingRadius = ringRadiiKm.FirstOrDefault(r => r > 0);

        var result = new List<CoastalInundationDto>();

        foreach (var (lat, lon, name) in PacificCoastPoints)
        {
            var distKm = InfrastructureService.HaversineKm(epicenterLat, epicenterLon, lat, lon);
            if (distKm > maxRadius || distKm < 1) continue;

            // Which ring first reaches this coast point
            var ringIdx = ringRadiiKm
                .Select((r, i) => (r, i))
                .FirstOrDefault(x => x.r >= distKm).i;

            // Wave height attenuates with cylindrical spreading: H ∝ 1/√r
            var localRunupM = firstRingRadius > 0
                ? estimatedRunupM * Math.Sqrt(firstRingRadius / distKm)
                : estimatedRunupM;
            localRunupM = Math.Max(0.1, localRunupM);

            // Inundation distance: d = runup / (slope * 1000 m/km)
            // Using mean Pacific coastal slope 0.5% — conservative for flat WA/OR plains
            var inundationKm = Math.Min(localRunupM / (CoastalSlopeFraction * 1000d), 15.0);
            inundationKm = Math.Max(0.05, inundationKm);

            // Population within the inundation zone (circle of inundationKm at coast point)
            var pop = _infrastructureService.GetPopulationWithinRadius(lat, lon, inundationKm);

            result.Add(new CoastalInundationDto
            {
                Lat                    = lat,
                Lon                    = lon,
                Name                   = name,
                DistanceFromEpicenterKm = Math.Round(distKm, 2),
                InundationKm           = Math.Round(inundationKm, 3),
                RunupM                 = Math.Round(localRunupM, 2),
                HitAtRingIndex         = ringIdx,
                AffectedPopulation     = pop
            });
        }

        return result;
    }

    // Empirical runup formula based on NOAA/Synolakis scaling:
    // Open-ocean amplitude: A ≈ 10^(0.5M - 3.5)  (empirical, Abe 1995)
    // Shoaling amplification: Green's law (h_deep / h_coastal)^0.25
    private static double EstimateRunupMeters(double magnitude)
    {
        var openOceanAmplitudeM = Math.Pow(10d, 0.5d * magnitude - 3.5d);
        const double deepWaterDepthM   = 4000d;
        const double coastalDepthM     = 10d;
        return openOceanAmplitudeM * Math.Pow(deepWaterDepthM / coastalDepthM, 0.25d);
    }
}
