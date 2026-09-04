# Overview

## Product

Flexis. A multi-feature web platform. The first screen for visitors is an editorial landing at `/`. Signed-in visits to `/` or `/sign-in` go to Dashboard. Sign-in and role-based access are in place. After sign-in the product shell is Dashboard, Job Application, Mail Check, Logs, Settings, and Help. Dashboard is a workspace status board for the signed-in account: platform health, Google client and Gmail state, listing and price KPIs, status mix, pipeline contribution, setup attention, recent activity, and Admin user counts. The AppBar uses client integration readiness, Google sync freshness, Gmail connection status, and an account menu. Job Application uses tabs; Operations is a Pipeline table that copies source location listings onto a profile sheet, can archive that sheet as a numbered log, and opens a row detail page for Update, Forward, and banned companies; Profiles lists profile name, source, sheet link, and rates, and opens a profile page with apply status and profile info; Financial prices each pipeline row from profile Status counts. Logs is a left-nav page with Job Application and Mail Check activity tabs, both server-paged. Job Application settings (Gmail connect, default apply and bonus rates, profile and source management, source location tabs) live on Settings. Creating a profile or source creates a Google Sheet under `Flexis` / `Job Application` / `Profiles` or `Sources` in that user's Drive. Those Job Application actions stay disabled until the signed-in user connects Gmail. Mail Check has its own mailbox connections on Settings (Mail Check). It labels or categorizes interview mail, pins it, and trashes application receipts using the user's OpenAI API key. Need action, Inbox, Check, and Usage are the Mail Check tabs. Admin Settings holds the Flexis Google Cloud client and the other users. Product Settings always has the signed-in account profile. Help is a tabbed guide. Overview is the product map. Google setup, Microsoft setup, Operations, Financial, Logs, Mail Check, and Problems are the topic guides.

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
tools/ngrok/
docker-compose.yml
run-public.bat
stop-public.bat
```

## Local run

Requires .NET 10 SDK and Node.js with PostgreSQL and MongoDB available locally.

- Frontend: [http://127.0.0.1:5173/](http://127.0.0.1:5173/)
- API health: [http://localhost:5080/api/health](http://localhost:5080/api/health)
- Sign in: `admin@flexis.local` / `FlexisAdmin1!`
- Public HTTPS URL (optional): start API and frontend, then `run-public.bat` — see [backend/public-url-ngrok.md](backend/public-url-ngrok.md)

## Related

- [conventions.md](conventions.md)
- [frontend/overview.md](frontend/overview.md)
- [backend/overview.md](backend/overview.md)
