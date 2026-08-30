# Conventions

Cross-cutting rules. Layer-specific detail lives under `frontend/` or `backend/`.

## Naming

- C# types: PascalCase. Async methods end with `Async` except controller actions.
- TypeScript: PascalCase components and types, camelCase functions and fields.
- Feature folders match the feature name (`features/health`).
- Solution projects use the `Flexis.` prefix.

## Errors

- API controllers return HTTP status codes with JSON bodies. Unhealthy health checks return `503`. Application exceptions map to Problem Details.
- Frontend API helper `getJson` throws on non-OK responses except `503` (parsed as a health payload). Other failures throw `ApiError`.
- Do not swallow exceptions. Health checks convert connection failures into an unhealthy result.

## Environment and config

| Name | Where | Purpose |
| --- | --- | --- |
| `ConnectionStrings:Postgres` | `backend/src/Flexis.Api/appsettings*.json` | Npgsql connection |
| `Mongo:ConnectionString` | same | MongoDB URI |
| `Mongo:Database` | same | MongoDB database name (`flexis`) |
| `Frontend:Origins` | same | CORS allowed browser origins |
| `Jwt:Issuer` `Jwt:Audience` `Jwt:SigningKey` `Jwt:AccessTokenMinutes` | same | JWT access tokens |
| `Auth:Seed:*` | `appsettings.Development.json` | Development seed users |
| `Google:RedirectUri` `Google:TokenProtectionKey` | `backend/src/Flexis.Api/appsettings*.json` | OAuth callback and token AES-GCM key |
| `Google:ClientId` `Google:ClientSecret` | same, optional fallback | Used only if Settings has no saved Google Cloud client |
| `VITE_API_BASE_URL` | `frontend/.env.development` | API base URL; empty in Development so Vite proxies `/api` |
| `ASPNETCORE_ENVIRONMENT` | launchSettings | `Development` locally |

Local database credentials match `docker-compose.yml` (`flexis` / `flexis`). Do not commit production secrets.

## Shared types and constants

Health contract is `HealthStatusDto` / `HealthCheckDto` in `Flexis.Application.Health`, mirrored in `frontend/src/shared/types/health.ts`. User contract is `UserDto` in `Flexis.Application.Users`, mirrored in `frontend/src/shared/types/user.ts`. Google connection status is `GoogleConnectionStatusDto` in `Flexis.Application.Google`, mirrored in `frontend/src/shared/types/google.ts`. Google Cloud client public settings are `GoogleClientSettingsDto` in the same folder. Job catalog items are `JobCatalogItemDto` in `Flexis.Application.JobApplication`, mirrored in `frontend/src/shared/types/jobCatalog.ts`. Source locations are `SourceLocationDto` in the same application folder. Pipeline board and entries are `JobPipelineBoardDto` / `JobPipelineEntryDto` in the same folder, mirrored in `frontend/src/shared/types/pipeline.ts`.

## Related

- [overview.md](overview.md)
- [decisions/README.md](decisions/README.md)
