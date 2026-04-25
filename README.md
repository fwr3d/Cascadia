# Cascadia - Real-Time Pacific Tsunami Simulator

## Structure

```text
cascadia/
|- frontend/      Next.js app for the landing page and simulator UI
\- Cascadia.Api/  ASP.NET Core API for simulation and infrastructure data
```

## Local development

### Frontend

```powershell
cd frontend
npm install
npm run dev
```

Required env in `frontend/.env.local`:

```env
NEXT_PUBLIC_MAPBOX_TOKEN=pk.xxx
NEXT_PUBLIC_API_URL=http://localhost:5055
```

### Backend

```powershell
cd Cascadia.Api
dotnet run
```

Default local backend URL:

```text
http://localhost:5055
```

Health endpoint:

```text
GET /api/health
```

## Deploy

### Vercel

Deploy `frontend/` as a separate Vercel project.

Set the Vercel project root directory to:

```text
frontend
```

Set these environment variables in Vercel:

```env
NEXT_PUBLIC_MAPBOX_TOKEN=pk.xxx
NEXT_PUBLIC_API_URL=https://your-backend.up.railway.app
```

### Railway

Deploy `Cascadia.Api/` as a separate Railway service.

Set the Railway service root directory to:

```text
Cascadia.Api
```

The backend includes a Dockerfile for Railway deployment.

Set these environment variables in Railway:

```env
FRONTEND_ORIGIN=https://your-frontend.vercel.app
```

Optional, if you want multiple allowed origins:

```env
CORS_ALLOWED_ORIGINS=https://your-frontend.vercel.app,https://your-preview.vercel.app,https://*.vercel.app
```

## Notes

- The frontend is configured to call the real backend by default.
- Backend CORS now supports `FRONTEND_ORIGIN` and optional comma-separated `CORS_ALLOWED_ORIGINS`.
- The old `backend/` folder is legacy and should not be used for deployment.
