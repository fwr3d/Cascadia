using System.Globalization;
using Cascadia.Api.Models;

namespace Cascadia.Api.Services;

public sealed class SimulationService
{
    private static readonly int[] RingMinutes = [0, 5, 10, 20, 30, 45, 60];
    private readonly InfrastructureService _infrastructureService;

    public SimulationService(InfrastructureService infrastructureService)
    {
        _infrastructureService = infrastructureService;
    }

    public async Task<SimulationResponseDto> SimulateAsync(
        SimulationRequestDto request,
        CancellationToken cancellationToken)
    {
        var waveSpeedKmS = Math.Sqrt(9.81d * (request.DepthKm * 1000d)) / 1000d;
        var energyJoules = Math.Pow(10d, 4.8d + (1.5d * request.Magnitude))
            .ToString("0.00e+00", CultureInfo.InvariantCulture);

        var ringSnapshots = new List<WaveRingDto>(RingMinutes.Length);
        var ringRadiiKm = new List<double>(RingMinutes.Length);

        foreach (var etaMinutes in RingMinutes)
        {
            var radiusKm = waveSpeedKmS * etaMinutes * 60d;
            ringRadiiKm.Add(radiusKm);

            var affectedCounties = await _infrastructureService.GetAffectedCountiesAsync(
                request.EpicenterLat,
                request.EpicenterLon,
                radiusKm,
                cancellationToken);

            ringSnapshots.Add(new WaveRingDto
            {
                RadiusKm = Math.Round(radiusKm, 2),
                EtaMinutes = etaMinutes,
                AffectedCounties = affectedCounties.ToList()
            });
        }

        var maxRadiusKm = ringRadiiKm.Count == 0 ? 0 : ringRadiiKm.Max();
        var affectedPopulation = await _infrastructureService.GetAffectedPopulationAsync(
            request.EpicenterLat,
            request.EpicenterLon,
            maxRadiusKm,
            cancellationToken);

        var infrastructureAtRisk = _infrastructureService.GetInfrastructureAtRisk(
            request.EpicenterLat,
            request.EpicenterLon,
            ringRadiiKm);

        var nearestCoastDistanceKm = _infrastructureService.GetNearestCoastDistanceKm(
            request.EpicenterLat,
            request.EpicenterLon);

        var etaNearestCoastMin = nearestCoastDistanceKm is null
            ? 0
            : Math.Round(nearestCoastDistanceKm.Value / (waveSpeedKmS * 60d), 2);

        return new SimulationResponseDto
        {
            WaveSpeedKmS = Math.Round(waveSpeedKmS, 3),
            EnergyJoules = energyJoules,
            EtaNearestCoastMin = etaNearestCoastMin,
            EstimatedRunupM = Math.Round(EstimateRunupMeters(request.Magnitude), 2),
            AffectedPopulation = affectedPopulation,
            Rings = ringSnapshots,
            InfrastructureAtRisk = infrastructureAtRisk.ToList()
        };
    }

    private static double EstimateRunupMeters(double magnitude)
    {
        const double deepWaterDepthMeters = 4000d;
        const double coastalDepthMeters = 10d;
        var deepWaterWaveHeightMeters = 0.01d * magnitude;
        return deepWaterWaveHeightMeters * Math.Pow(deepWaterDepthMeters / coastalDepthMeters, 0.25d);
    }
}
