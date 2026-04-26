using System.Globalization;
using Cascadia.Api.Models;

namespace Cascadia.Api.Services;

public sealed class SimulationService
{
    // Ring times in minutes — local coast focus, capped at 30 min
    private static readonly int[] RingMinutes = [0, 5, 10, 20, 30];

    // Mean Pacific slope for inundation: ~0.5% (0.005) — flat coastal plains WA/OR/CA
    private const double CoastalSlopeFraction = 0.005;

    // Coast reference points: lat, lon, name — Pacific, Atlantic, Gulf, Caribbean
    private static readonly (double Lat, double Lon, string Name)[] CoastPoints =
    [
        // Pacific — SE Alaska to Baja + Hawaii
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

        // Atlantic — Maine to Florida East
        (47.1,  -67.8,  "Eastport, ME"),
        (44.6,  -67.0,  "Machias, ME"),
        (44.4,  -68.2,  "Bar Harbor, ME"),
        (43.7,  -70.2,  "Portland, ME"),
        (43.1,  -70.8,  "Portsmouth, NH"),
        (42.9,  -70.8,  "Newburyport, MA"),
        (42.4,  -71.0,  "Boston, MA"),
        (41.9,  -70.0,  "Cape Cod, MA"),
        (41.5,  -71.3,  "Newport, RI"),
        (41.3,  -72.1,  "New Haven, CT"),
        (41.0,  -72.9,  "Long Island East, NY"),
        (40.7,  -73.9,  "New York City, NY"),
        (40.4,  -74.0,  "Sandy Hook, NJ"),
        (39.4,  -74.4,  "Atlantic City, NJ"),
        (38.9,  -74.9,  "Cape May, NJ"),
        (38.8,  -75.1,  "Lewes, DE"),
        (38.3,  -75.1,  "Ocean City, MD"),
        (37.9,  -75.4,  "Chincoteague, VA"),
        (37.0,  -76.0,  "Virginia Beach, VA"),
        (36.9,  -76.3,  "Norfolk, VA"),
        (35.9,  -75.6,  "Outer Banks, NC"),
        (34.7,  -76.7,  "Cape Lookout, NC"),
        (34.2,  -77.8,  "Wilmington, NC"),
        (33.9,  -78.0,  "Myrtle Beach, SC"),
        (32.8,  -79.9,  "Charleston, SC"),
        (32.1,  -80.9,  "Hilton Head, SC"),
        (31.1,  -81.4,  "Brunswick, GA"),
        (30.4,  -81.4,  "Jacksonville, FL"),
        (29.1,  -80.6,  "Daytona Beach, FL"),
        (28.4,  -80.6,  "Cape Canaveral, FL"),
        (27.7,  -80.4,  "Vero Beach, FL"),
        (26.7,  -80.1,  "West Palm Beach, FL"),
        (25.8,  -80.1,  "Miami, FL"),
        (25.2,  -80.3,  "Homestead, FL"),
        (24.6,  -81.8,  "Key West, FL"),

        // Gulf of Mexico — Florida West to Texas
        (25.5,  -81.4,  "Cape Sable, FL"),
        (26.1,  -81.8,  "Naples, FL"),
        (26.9,  -82.4,  "Fort Myers, FL"),
        (27.5,  -82.7,  "Sarasota, FL"),
        (27.9,  -82.8,  "Tampa Bay, FL"),
        (28.9,  -82.8,  "Crystal River, FL"),
        (29.8,  -84.4,  "Apalachee Bay, FL"),
        (30.2,  -85.9,  "Panama City, FL"),
        (30.4,  -87.2,  "Pensacola, FL"),
        (30.4,  -88.0,  "Mobile Bay, AL"),
        (30.4,  -89.0,  "Gulfport, MS"),
        (29.9,  -89.9,  "New Orleans, LA"),
        (29.7,  -91.2,  "Morgan City, LA"),
        (29.7,  -93.9,  "Lake Charles, LA"),
        (29.8,  -94.0,  "Beaumont, TX"),
        (29.7,  -94.8,  "Galveston, TX"),
        (28.8,  -95.8,  "Freeport, TX"),
        (28.0,  -97.1,  "Corpus Christi, TX"),
        (26.1,  -97.2,  "Brownsville, TX"),

        // Puerto Rico & US Virgin Islands
        (18.5,  -66.1,  "San Juan, PR"),
        (18.0,  -67.1,  "Mayaguez, PR"),
        (17.7,  -64.7,  "St. Thomas, USVI"),
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

        var estimatedRunupM = EstimateRunupMeters(request.Magnitude, request.DepthKm);

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

        foreach (var (lat, lon, name) in CoastPoints)
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

            // Skip negligible waves — 0.5m is the international advisory threshold
            if (localRunupM < 0.5) continue;

            localRunupM = Math.Max(0.5, localRunupM);

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
    // Depth efficiency: deeper ruptures displace less seafloor vertically.
    // Validated against 8 NOAA NGDC events — depth factor brings median ratio
    // from 3.9x to ~1.5x. Formula: exp(-(depth - 20) / 45), clamped to [0.05, 1.0].
    private static double EstimateRunupMeters(double magnitude, double depthKm)
    {
        var openOceanAmplitudeM = Math.Pow(10d, 0.5d * magnitude - 3.5d);
        const double deepWaterDepthM = 4000d;
        const double coastalDepthM   = 10d;
        var shoaling = Math.Pow(deepWaterDepthM / coastalDepthM, 0.25d);
        var depthEfficiency = Math.Clamp(Math.Exp(-(depthKm - 20d) / 45d), 0.05d, 1.0d);
        return openOceanAmplitudeM * shoaling * depthEfficiency;
    }
}
