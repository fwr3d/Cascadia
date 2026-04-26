using System.Globalization;
using System.Text;
using System.Text.Json;
using Cascadia.Api.Models;

namespace Cascadia.Api.Services;

public sealed class InfrastructureService
{
    private const string CensusPopulationUrl =
        "https://api.census.gov/data/2020/dec/pl?get=P1_001N,NAME&for=county:*&in=state:*";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InfrastructureService> _logger;
    private readonly string _contentRootPath;
    private readonly IReadOnlyList<InfrastructureCatalogItem> _infrastructure;
    private readonly SemaphoreSlim _countyLoadLock = new(1, 1);

    private IReadOnlyList<CountyProfile>? _counties;

    public InfrastructureService(
        IHostEnvironment environment,
        IHttpClientFactory httpClientFactory,
        ILogger<InfrastructureService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _contentRootPath = environment.ContentRootPath;
        _infrastructure = LoadInfrastructureCatalog();
    }

    public IReadOnlyList<InfrastructureItemDto> GetInfrastructureWithinRadius(double lat, double lon, double radiusKm)
    {
        return _infrastructure
            .Select(item => new
            {
                Item = item,
                DistanceKm = HaversineKm(lat, lon, item.Lat, item.Lon)
            })
            .Where(result => result.DistanceKm <= radiusKm)
            .OrderBy(result => result.DistanceKm)
            .Select(result => ToInfrastructureItemDto(result.Item, result.DistanceKm))
            .ToList();
    }

    public IReadOnlyList<InfrastructureRiskDto> GetInfrastructureAtRisk(
        double lat,
        double lon,
        IReadOnlyList<double> ringRadiiKm)
    {
        var maxRadiusKm = ringRadiiKm.Count == 0 ? 0 : ringRadiiKm.Max();

        return _infrastructure
            .Select(item => new
            {
                Item = item,
                DistanceKm = HaversineKm(lat, lon, item.Lat, item.Lon)
            })
            .Where(result => result.DistanceKm <= maxRadiusKm)
            .Select(result =>
            {
                var hitAtRingIndex = ringRadiiKm
                    .Select((radius, index) => new { radius, index })
                    .FirstOrDefault(entry => result.DistanceKm <= entry.radius)?.index ?? (ringRadiiKm.Count - 1);

                return new InfrastructureRiskDto
                {
                    Name = result.Item.Name,
                    Type = result.Item.Type,
                    Lat = result.Item.Lat,
                    Lon = result.Item.Lon,
                    DistanceKm = Math.Round(result.DistanceKm, 2),
                    GridCoverageRadiusKm = result.Item.GridCoverageRadiusKm,
                    HitAtRingIndex = hitAtRingIndex
                };
            })
            .OrderBy(item => item.HitAtRingIndex)
            .ThenBy(item => item.DistanceKm)
            .ToList();
    }

    public async Task<IReadOnlyList<AffectedCountyDto>> GetAffectedCountiesAsync(
        double lat,
        double lon,
        double radiusKm,
        CancellationToken cancellationToken)
    {
        var counties = await EnsureCountyProfilesAsync(cancellationToken);

        return counties
            .Select(county => new
            {
                County = county,
                DistanceKm = HaversineKm(lat, lon, county.Lat, county.Lon)
            })
            .Where(result => result.DistanceKm <= radiusKm)
            .OrderByDescending(result => result.County.Population)
            .Select(result => new AffectedCountyDto
            {
                Name = result.County.Name,
                State = result.County.State,
                Pop = result.County.Population,
                Fips = result.County.Fips
            })
            .ToList();
    }

    public async Task<int> GetAffectedPopulationAsync(
        double lat,
        double lon,
        double radiusKm,
        CancellationToken cancellationToken)
    {
        var counties = await GetAffectedCountiesAsync(lat, lon, radiusKm, cancellationToken);
        return counties.Sum(county => county.Pop);
    }

    public async Task<IReadOnlyList<AffectedCountyDto>> GetCountiesInInundationZonesAsync(
        IReadOnlyList<CoastalInundationDto> zones,
        CancellationToken cancellationToken)
    {
        if (zones.Count == 0) return [];

        var counties = await EnsureCountyProfilesAsync(cancellationToken);

        // WA/OR/CA coastal county centroids sit 25-55 km inland from the shoreline.
        // Base offset of 50 km ensures coastal counties are reachable; inundationKm
        // extends the radius further for larger events so magnitude still matters.
        const double centroidOffsetKm = 50.0;
        return counties
            .Where(county => zones.Any(zone =>
                HaversineKm(county.Lat, county.Lon, zone.Lat, zone.Lon) <= zone.InundationKm + centroidOffsetKm))
            .OrderByDescending(county => county.Population)
            .Select(county => new AffectedCountyDto
            {
                Name = county.Name,
                State = county.State,
                Pop = county.Population,
                Fips = county.Fips
            })
            .ToList();
    }

    public int GetPopulationWithinRadius(double lat, double lon, double radiusKm)
    {
        if (_counties is null) return 0;

        const double centroidOffsetKm = 50.0;
        return _counties
            .Where(county => HaversineKm(lat, lon, county.Lat, county.Lon) <= radiusKm + centroidOffsetKm)
            .Sum(county => county.Population);
    }

    public Task WarmupAsync(CancellationToken cancellationToken = default) =>
        EnsureCountyProfilesAsync(cancellationToken);

    public double? GetNearestCoastDistanceKm(double lat, double lon)
    {
        var coastPoints = _infrastructure.Where(item => item.Type == "port").ToList();
        if (coastPoints.Count == 0)
        {
            return null;
        }

        return coastPoints.Min(point => HaversineKm(lat, lon, point.Lat, point.Lon));
    }

    private async Task<IReadOnlyList<CountyProfile>> EnsureCountyProfilesAsync(CancellationToken cancellationToken)
    {
        if (_counties is not null)
        {
            return _counties;
        }

        await _countyLoadLock.WaitAsync(cancellationToken);
        try
        {
            if (_counties is not null)
            {
                return _counties;
            }

            _counties = await LoadCountyProfilesAsync(cancellationToken);
            return _counties;
        }
        finally
        {
            _countyLoadLock.Release();
        }
    }

    private async Task<IReadOnlyList<CountyProfile>> LoadCountyProfilesAsync(CancellationToken cancellationToken)
    {
        var localJson = TryLoadCountiesFromJson();
        if (localJson.Count > 0)
        {
            return localJson;
        }

        var localCsv = await TryLoadCountiesFromCsvAsync(cancellationToken);
        if (localCsv.Count > 0)
        {
            return localCsv;
        }

        _logger.LogInformation("Using built-in fallback county profiles. Add Data/counties.json or Data/county-centroids.csv for broader coverage.");
        return GetFallbackCountyProfiles();
    }

    private IReadOnlyList<CountyProfile> TryLoadCountiesFromJson()
    {
        var path = Path.Combine(_contentRootPath, "Data", "counties.json");
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var counties = new List<CountyProfile>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                var fips = ReadString(element, "fips");
                var name = ReadString(element, "name", "county");
                var state = ReadString(element, "state");
                var lat = ReadDouble(element, "lat", "latitude");
                var lon = ReadDouble(element, "lon", "longitude");
                var pop = ReadInt(element, "pop", "population");

                if (string.IsNullOrWhiteSpace(fips) ||
                    string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(state) ||
                    lat is null ||
                    lon is null ||
                    pop is null)
                {
                    continue;
                }

                counties.Add(new CountyProfile(fips, name, state, lat.Value, lon.Value, pop.Value));
            }

            return counties;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Data/counties.json. Falling back to other county sources.");
            return [];
        }
    }

    private async Task<IReadOnlyList<CountyProfile>> TryLoadCountiesFromCsvAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_contentRootPath, "Data", "county-centroids.csv");
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            var populationByFips = await TryLoadPopulationByFipsAsync(cancellationToken);
            if (populationByFips.Count == 0)
            {
                return [];
            }

            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
            if (lines.Length < 2)
            {
                return [];
            }

            var headers = SplitCsvLine(lines[0]);
            var fipsIndex = FindHeaderIndex(headers, "fips");
            var latIndex = FindHeaderIndex(headers, "lat", "latitude");
            var lonIndex = FindHeaderIndex(headers, "lon", "longitude", "lng");
            var nameIndex = FindHeaderIndex(headers, "name", "county");
            var stateIndex = FindHeaderIndex(headers, "state", "state_name", "stusab");

            if (fipsIndex < 0 || latIndex < 0 || lonIndex < 0 || nameIndex < 0 || stateIndex < 0)
            {
                return [];
            }

            var counties = new List<CountyProfile>();

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var columns = SplitCsvLine(line);
                if (columns.Count <= Math.Max(Math.Max(fipsIndex, latIndex), Math.Max(lonIndex, Math.Max(nameIndex, stateIndex))))
                {
                    continue;
                }

                var fips = columns[fipsIndex].Trim();
                if (!populationByFips.TryGetValue(fips, out var population))
                {
                    continue;
                }

                if (!double.TryParse(columns[latIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                    !double.TryParse(columns[lonIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                {
                    continue;
                }

                var name = columns[nameIndex].Trim().Trim('"');
                var state = columns[stateIndex].Trim().Trim('"');
                counties.Add(new CountyProfile(fips, name, state, lat, lon, population));
            }

            return counties;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Data/county-centroids.csv. Falling back to built-in county profiles.");
            return [];
        }
    }

    private async Task<Dictionary<string, int>> TryLoadPopulationByFipsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(CensusPopulationUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var populationByFips = new Dictionary<string, int>();
            foreach (var row in document.RootElement.EnumerateArray().Skip(1))
            {
                if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() < 4)
                {
                    continue;
                }

                var populationText = row[0].GetString();
                var stateCode = row[2].GetString();
                var countyCode = row[3].GetString();

                if (string.IsNullOrWhiteSpace(populationText) ||
                    string.IsNullOrWhiteSpace(stateCode) ||
                    string.IsNullOrWhiteSpace(countyCode) ||
                    !int.TryParse(populationText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var population))
                {
                    continue;
                }

                populationByFips[stateCode + countyCode] = population;
            }

            return populationByFips;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch census population data. Built-in county populations will be used.");
            return [];
        }
    }

    private IReadOnlyList<InfrastructureCatalogItem> LoadInfrastructureCatalog()
    {
        var hospitals = LoadInfrastructureFile("hospitals.json", "hospital");
        var powerPlants = LoadInfrastructureFile("power-plants.json", "powerPlant");
        var ports = LoadInfrastructureFile("ports.json", "port");

        return
        [
            ..(hospitals.Count > 0 ? hospitals : GetFallbackHospitals()),
            ..(powerPlants.Count > 0 ? powerPlants : GetFallbackPowerPlants()),
            ..(ports.Count > 0 ? ports : GetFallbackPorts()),
        ];
    }

    private IReadOnlyList<InfrastructureCatalogItem> LoadInfrastructureFile(string fileName, string type)
    {
        var path = Path.Combine(_contentRootPath, "Data", fileName);
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);

            var items = new List<InfrastructureCatalogItem>();
            foreach (var element in EnumerateJsonItems(document.RootElement))
            {
                var name = ReadString(element, "name", "facility_name", "plant_name", "port_name");
                var lat = ReadDouble(element, "lat", "latitude", "y");
                var lon = ReadDouble(element, "lon", "longitude", "x");
                var capacityMw = ReadDouble(element, "capacityMw", "capacity_mw", "total_mw");
                var beds = ReadInt(element, "beds", "bed_count", "staffed_beds");

                if ((lat is null || lon is null) && TryGetNestedObject(element, out var nested))
                {
                    name ??= ReadString(nested, "name", "facility_name", "plant_name", "port_name");
                    capacityMw ??= ReadDouble(nested, "capacityMw", "capacity_mw", "total_mw");
                    beds ??= ReadInt(nested, "beds", "bed_count", "staffed_beds");
                    lat ??= ReadDouble(nested, "lat", "latitude", "y");
                    lon ??= ReadDouble(nested, "lon", "longitude", "x");
                }

                if ((lat is null || lon is null) && TryGetProperty(element, "geometry", out var geometry))
                {
                    if (TryGetProperty(geometry, "coordinates", out var coordinates) &&
                        coordinates.ValueKind == JsonValueKind.Array &&
                        coordinates.GetArrayLength() >= 2)
                    {
                        lon ??= coordinates[0].TryGetDouble(out var coordLon) ? coordLon : null;
                        lat ??= coordinates[1].TryGetDouble(out var coordLat) ? coordLat : null;
                    }

                    lat ??= ReadDouble(geometry, "lat", "latitude", "y");
                    lon ??= ReadDouble(geometry, "lon", "longitude", "x");
                }

                if (string.IsNullOrWhiteSpace(name) || lat is null || lon is null)
                {
                    continue;
                }

                items.Add(new InfrastructureCatalogItem(
                    name,
                    type,
                    lat.Value,
                    lon.Value,
                    ComputeGridCoverageRadiusKm(type, capacityMw),
                    capacityMw,
                    beds));
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load infrastructure file {FileName}. Using fallback data instead.", fileName);
            return [];
        }
    }

    private static IEnumerable<JsonElement> EnumerateJsonItems(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                yield return element;
            }

            yield break;
        }

        if (root.ValueKind == JsonValueKind.Object &&
            TryGetProperty(root, "features", out var features) &&
            features.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in features.EnumerateArray())
            {
                yield return element;
            }
        }
    }

    private static bool TryGetNestedObject(JsonElement element, out JsonElement nested)
    {
        if (TryGetProperty(element, "properties", out nested) && nested.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        if (TryGetProperty(element, "attributes", out nested) && nested.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        nested = default;
        return false;
    }

    private static InfrastructureItemDto ToInfrastructureItemDto(InfrastructureCatalogItem item, double distanceKm)
    {
        return new InfrastructureItemDto
        {
            Name = item.Name,
            Type = item.Type,
            Lat = item.Lat,
            Lon = item.Lon,
            DistanceKm = Math.Round(distanceKm, 2),
            GridCoverageRadiusKm = item.GridCoverageRadiusKm,
            CapacityMw = item.CapacityMw,
            Beds = item.Beds
        };
    }

    private static double ComputeGridCoverageRadiusKm(string type, double? capacityMw)
    {
        if (type != "powerPlant" || capacityMw is null)
        {
            return 0;
        }

        return Math.Round(Math.Clamp(30 + ((capacityMw.Value / 1000d) * 50), 30, 200), 2);
    }

    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }

            return property.ToString();
        }

        return null;
    }

    private static double? ReadDouble(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var number))
            {
                return number;
            }

            if (property.ValueKind == JsonValueKind.String &&
                double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static int? ReadInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
            {
                return number;
            }

            if (property.ValueKind == JsonValueKind.String &&
                int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement property)
    {
        foreach (var candidate in element.EnumerateObject())
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var insideQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                insideQuotes = !insideQuotes;
                continue;
            }

            if (ch == ',' && !insideQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values;
    }

    private static int FindHeaderIndex(IReadOnlyList<string> headers, params string[] candidates)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            var header = headers[index].Trim().Trim('"');
            if (candidates.Any(candidate => string.Equals(header, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return index;
            }
        }

        return -1;
    }

    private static IReadOnlyList<CountyProfile> GetFallbackCountyProfiles()
    {
        return
        [
            new("02020", "Anchorage Municipality", "AK", 61.1743, -149.2843, 291247),
            new("02122", "Kenai Peninsula Borough", "AK", 60.3819, -151.2659, 58799),
            new("02150", "Kodiak Island Borough", "AK", 57.5536, -153.7498, 13005),
            new("02170", "Matanuska-Susitna Borough", "AK", 61.5895, -149.1246, 107801),
            new("02013", "Aleutians East Borough", "AK", 55.3202, -161.9625, 3370),
            new("41007", "Clatsop County", "OR", 46.1199, -123.7608, 41606),
            new("41011", "Coos County", "OR", 43.3665, -124.2179, 64929),
            new("41015", "Curry County", "OR", 42.4563, -124.1754, 23524),
            new("41039", "Lane County", "OR", 43.9106, -123.1123, 382971),
            new("41051", "Multnomah County", "OR", 45.5149, -122.4148, 815428),
            new("53009", "Clallam County", "WA", 48.0418, -123.8171, 77977),
            new("53027", "Grays Harbor County", "WA", 46.9787, -123.8294, 75932),
            new("53033", "King County", "WA", 47.4914, -121.8339, 2269675),
            new("53053", "Pierce County", "WA", 47.0391, -122.1295, 921130),
            new("53061", "Snohomish County", "WA", 48.0473, -121.7173, 827957),
            new("06015", "Del Norte County", "CA", 41.7425, -123.8974, 27912),
            new("06023", "Humboldt County", "CA", 40.7067, -123.9262, 136463),
            new("06037", "Los Angeles County", "CA", 34.1966, -118.261, 10014009),
            new("06075", "San Francisco County", "CA", 37.7558, -122.4431, 873965),
            new("06073", "San Diego County", "CA", 32.8242, -117.149, 3298634),
            new("15003", "Honolulu County", "HI", 21.3156, -157.8581, 1016508),
            new("15009", "Maui County", "HI", 20.7984, -156.3319, 164754)
        ];
    }

    private static IReadOnlyList<InfrastructureCatalogItem> GetFallbackPowerPlants()
    {
        return
        [
            new("Diablo Canyon Power Plant", "powerPlant", 35.2114, -120.8562, ComputeGridCoverageRadiusKm("powerPlant", 2240), 2240, null),
            new("Moss Landing Power Plant", "powerPlant", 36.8075, -121.7908, ComputeGridCoverageRadiusKm("powerPlant", 2560), 2560, null),
            new("Haynes Generating Station", "powerPlant", 33.7659, -118.2043, ComputeGridCoverageRadiusKm("powerPlant", 1540), 1540, null),
            new("Scattergood Generating Station", "powerPlant", 33.9356, -118.4414, ComputeGridCoverageRadiusKm("powerPlant", 1630), 1630, null),
            new("Columbia Generating Station", "powerPlant", 46.4711, -119.3335, ComputeGridCoverageRadiusKm("powerPlant", 1190), 1190, null),
            new("Chehalis Generation Facility", "powerPlant", 46.6353, -122.9649, ComputeGridCoverageRadiusKm("powerPlant", 520), 520, null),
            new("Carty Generating Station", "powerPlant", 45.7261, -121.6318, ComputeGridCoverageRadiusKm("powerPlant", 440), 440, null),
            new("Centralia Big Hanaford Plant", "powerPlant", 46.7275, -122.9545, ComputeGridCoverageRadiusKm("powerPlant", 1340), 1340, null),
            new("South Anchorage Substation", "powerPlant", 61.1404, -149.8759, ComputeGridCoverageRadiusKm("powerPlant", 350), 350, null),
            new("Kahe Power Plant", "powerPlant", 21.3473, -158.1227, ComputeGridCoverageRadiusKm("powerPlant", 650), 650, null)
        ];
    }

    private static IReadOnlyList<InfrastructureCatalogItem> GetFallbackHospitals()
    {
        return
        [
            new("Harborview Medical Center", "hospital", 47.6043, -122.3257, 0, null, 413),
            new("UW Medical Center", "hospital", 47.6495, -122.3044, 0, null, 570),
            new("Virginia Mason Medical Center", "hospital", 47.6101, -122.3277, 0, null, 336),
            new("Tacoma General Hospital", "hospital", 47.2528, -122.4415, 0, null, 437),
            new("Oregon Health and Science University Hospital", "hospital", 45.4997, -122.6863, 0, null, 576),
            new("Providence St. Vincent Medical Center", "hospital", 45.5096, -122.7737, 0, null, 523),
            new("PeaceHealth Sacred Heart Medical Center", "hospital", 44.0582, -123.0884, 0, null, 388),
            new("Providence Alaska Medical Center", "hospital", 61.1896, -149.8075, 0, null, 401),
            new("Alaska Regional Hospital", "hospital", 61.2013, -149.7851, 0, null, 250),
            new("UCSF Medical Center", "hospital", 37.7621, -122.4586, 0, null, 600),
            new("UC San Diego Medical Center", "hospital", 32.7541, -117.1662, 0, null, 808),
            new("The Queen's Medical Center", "hospital", 21.3077, -157.8524, 0, null, 575)
        ];
    }

    private static IReadOnlyList<InfrastructureCatalogItem> GetFallbackPorts()
    {
        return
        [
            new("Port of Anchorage", "port", 61.2351, -149.8857, 0, null, null),
            new("Port of Kodiak", "port", 57.7903, -152.4072, 0, null, null),
            new("Port of Seattle", "port", 47.6026, -122.3375, 0, null, null),
            new("Port of Tacoma", "port", 47.2649, -122.4137, 0, null, null),
            new("Port of Portland", "port", 45.5887, -122.7604, 0, null, null),
            new("Port of Coos Bay", "port", 43.3665, -124.2174, 0, null, null),
            new("Port of San Francisco", "port", 37.808, -122.4652, 0, null, null),
            new("Port of Los Angeles", "port", 33.7361, -118.2631, 0, null, null),
            new("Port of San Diego", "port", 32.7091, -117.1688, 0, null, null),
            new("Port of Honolulu", "port", 21.3069, -157.8677, 0, null, null)
        ];
    }

    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var lat1Rad = DegreesToRadians(lat1);
        var lat2Rad = DegreesToRadians(lat2);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * Math.Pow(Math.Sin(dLon / 2), 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180d);

    private sealed record CountyProfile(string Fips, string Name, string State, double Lat, double Lon, int Population);

    private sealed record InfrastructureCatalogItem(
        string Name,
        string Type,
        double Lat,
        double Lon,
        double GridCoverageRadiusKm,
        double? CapacityMw,
        int? Beds);
}
