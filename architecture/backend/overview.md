# Backend overview

## Role

HTTP API for Flexis. Owns business use cases, PostgreSQL, and MongoDB. Serves JSON to the frontend.

## Tech stack

ASP.NET Core 10 (`net10.0`), EF Core with Npgsql, MongoDB.Driver, JWT Bearer. Solution: `backend/Flexis.sln`.

## Entry

`backend/src/Flexis.Api/Program.cs`. Listens on `http://localhost:5080` in Development (`Properties/launchSettings.json`). Development applies EF migrations and seeds users before accepting requests.

## Boundaries

- `Flexis.Domain` — entities and domain rules, including `User`, `GoogleConnection`, `GoogleClientCredentials`, `JobCatalogItem`, `JobPipelineEntry`, `JobPipelineBannedCompany`, `JobFinancialSettings`, and `JobApplicationLog`.
- `Flexis.Application` — use cases, DTOs, DI entry `AddApplication`.
- `Flexis.Infrastructure` — EF Core, MongoDB, JWT, password hashing, Google OAuth, Google Sheets, Google Drive, health checks, DI entry `AddInfrastructure`.
- `Flexis.Api` — HTTP, CORS, JWT bearer, OpenAPI in Development, controllers.

Dependencies flow inward: Api → Application and Infrastructure; Infrastructure → Application and Domain; Application → Domain.

## Related

- [structure.md](structure.md)
- [api.md](api.md)
- [data.md](data.md)
- [security.md](security.md)
- [../frontend/overview.md](../frontend/overview.md)
