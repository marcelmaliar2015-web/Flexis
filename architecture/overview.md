# Overview

## Product

Flexis. A multi-feature web platform. The first screen for visitors is an editorial landing at `/`. Signed-in visits to `/` or `/sign-in` go to Dashboard. Sign-in and role-based access are in place. After sign-in the product shell is Dashboard, Job Application, Settings, and Help. Dashboard is a workspace status board for the signed-in account: platform health, Google client and Gmail state, listing and price KPIs, status mix, pipeline contribution, setup attention, recent activity, and Admin user counts. The AppBar uses Google sync freshness, Gmail connection status, and an account menu. Job Application uses tabs; Operations is a Pipeline table that copies source location listings onto a profile sheet, can archive that sheet as a numbered log, and opens a row detail page for banned companies; Financial prices each pipeline row from profile Status counts; Logs is a dated activity feed; Settings has Gmail connect, default apply and bonus rates, profile and source management, and source location tabs. Creating a profile or source creates a Google Sheet under `Flexis` / `Job Application` / `Profiles` or `Sources` in that user's Drive. Those Job Application actions stay disabled until the signed-in user connects Gmail. Admin Settings holds the Flexis Google Cloud client and the other users. Product Settings always has the signed-in account profile. Help is a tabbed guide. Overview is the product map. Google setup, Operations, Financial, Logs, and Problems are the topic guides.

## System context

The browser loads the React app. The app calls the ASP.NET Core HTTP API. The API uses PostgreSQL for relational data and MongoDB for document data.

## Tech stack

| Layer | Choice |
| --- | --- |
| Frontend | React 19, TypeScript, Vite, MUI, TanStack Query, React Router |
| Backend | ASP.NET Core 10, C# |
| Data | PostgreSQL 16 (EF Core), MongoDB 7 (MongoDB.Driver) |
| Infra | Docker Compose for local databases |

## Repository layout

```
architecture/
backend/
  Flexis.sln
  Directory.Build.props
  src/Flexis.Api
  src/Flexis.Application
  src/Flexis.Domain
  src/Flexis.Infrastructure
frontend/
docker-compose.yml
```

## Local run

Requires .NET 10 SDK, Node.js, and Docker Desktop (or PostgreSQL 16 and MongoDB 7 on the same ports).

1. `docker compose up -d` (PostgreSQL `localhost:5432`, MongoDB `localhost:27017`)
2. `dotnet run --project backend/src/Flexis.Api` (API `http://localhost:5080`)
3. `npm run dev` in `frontend/` (app `http://127.0.0.1:5173`)

## Related

- [conventions.md](conventions.md)
- [frontend/overview.md](frontend/overview.md)
- [backend/overview.md](backend/overview.md)
