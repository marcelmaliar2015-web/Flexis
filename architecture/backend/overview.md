# Backend overview

## Role

HTTP API for Flexis. Owns business use cases, PostgreSQL, and MongoDB. Serves JSON to the frontend.

## Tech stack

ASP.NET Core 10 (`net10.0`), EF Core with Npgsql, MongoDB.Driver. Solution: `backend/Flexis.sln`.

## Entry

`backend/src/Flexis.Api/Program.cs`. Listens on `http://localhost:5080` in Development (`Properties/launchSettings.json`).

## Boundaries

- `Flexis.Domain` — entities and domain rules. No types yet.
- `Flexis.Application` — use cases, DTOs, DI entry `AddApplication`.
- `Flexis.Infrastructure` — EF Core, MongoDB, health checks, DI entry `AddInfrastructure`.
- `Flexis.Api` — HTTP, CORS, OpenAPI in Development, controllers.

Dependencies flow inward: Api → Application and Infrastructure; Infrastructure → Application and Domain; Application → Domain.

## Related

- [structure.md](structure.md)
- [api.md](api.md)
- [data.md](data.md)
- [security.md](security.md)
- [../frontend/overview.md](../frontend/overview.md)
