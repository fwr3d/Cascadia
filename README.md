# Cascadia — Real-Time Pacific Tsunami Simulator

## Structure

```
cascadia/
├── frontend/   Next.js 14 app (you)
└── backend/    C# .NET 8 Web API (partner)
```

## Frontend

```bash
cd frontend
npm install
npm run dev        # http://localhost:3000
```

Requires `frontend/.env.local` with `NEXT_PUBLIC_MAPBOX_TOKEN=pk.xxx`

## Backend

```bash
cd backend
dotnet new webapi -n CascadiaApi --use-minimal-apis
# copy Program.cs, Models.cs, SimulationService.cs into CascadiaApi/
dotnet run         # http://localhost:5000
```

When the backend is ready, set `USE_MOCK = false` in `frontend/lib/api.ts`.
