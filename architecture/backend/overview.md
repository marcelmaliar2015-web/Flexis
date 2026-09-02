# Backend overview

## Role

HTTP API for Flexis. Owns business use cases, PostgreSQL, and MongoDB. Serves JSON to the frontend.

## Tech stack

ASP.NET Core 10 (`net10.0`), EF Core with Npgsql, MongoDB.Driver, JWT Bearer. Solution: `backend/Flexis.sln`.

## Entry

`backend/src/Flexis.Api/Program.cs`. Listens on `http://localhost:5080` in Development (`Properties/launchSettings.json`). Local dev: double-click `backend/run.bat`. It starts flexis-db PostgreSQL and MongoDB in `%LOCALAPPDATA%\flexis-db` when those binaries exist, otherwise Docker Compose, stops any stale API on port 5080, then runs `dotnet watch`. Falls back to `%LOCALAPPDATA%\dotnet-flexis\dotnet.exe` when `dotnet` is not on PATH. Development applies EF migrations and seeds users before accepting requests.

## Boundaries

- `Flexis.Domain` — entities and domain rules, including `User`, `GoogleConnection`, `GoogleClientCredentials`, `JobCatalogItem`, `JobPipelineEntry`, `JobProfileBannedCompany`, `JobFinancialSettings`, `JobApplicationLog`, `MailCheckSettings`, and `MailCheckProcessedMessage`.
- `Flexis.Application` — use cases, DTOs, DI entry `AddApplication`.
- `Flexis.Infrastructure` — EF Core, MongoDB, JWT, password hashing, Google OAuth, Google Sheets, Google Drive, Gmail, OpenAI, issue log, health checks, DI entry `AddInfrastructure`.
- `Flexis.Api` — HTTP, CORS, JWT bearer, OpenAPI in Development, controllers.

Dependencies flow inward: Api → Application and Infrastructure; Infrastructure → Application and Domain; Application → Domain.

## Related

- [structure.md](structure.md)
- [api.md](api.md)
- [data.md](data.md)
- [security.md](security.md)
- [public-url-ngrok.md](public-url-ngrok.md)
- [../frontend/overview.md](../frontend/overview.md)
