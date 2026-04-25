# Cascadia Backend

C# .NET 8 Web API. Run with `dotnet run` from this directory.

## Setup

```bash
dotnet new webapi -n CascadiaApi
cd CascadiaApi
dotnet run
```

Runs on `http://localhost:5000` by default.

## Required endpoint

`POST /api/simulate`

Request body:
```json
{
  "epicenterLat": 60.91,
  "epicenterLon": -147.34,
  "magnitude": 9.2,
  "depthKm": 25
}
```

Response: see `CascadiaApi/Models/SimulateResponse.cs`

## Key physics

- Wave speed: `v = sqrt(g * depth)` where depth is ocean depth at epicenter
- Energy: `E = 10^(1.5 * M + 4.8)` joules (Gutenberg-Richter)
- ETA: `distanceKm / (v_km_per_min)`
- Wave height decay: `H(r) = H0 * (r0 / r)^0.5`
