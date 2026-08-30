# Backend API

## Style

ASP.NET Core controllers. JSON camelCase. Route prefix `api/`. CORS policy `Frontend` allows `Frontend:Origins`. OpenAPI mapped in Development at `/openapi/v1.json`.

## Endpoints

| Method | Path | Response |
| --- | --- | --- |
| GET | `/api/health` | `200` + `HealthStatusDto` when both checks pass; `503` + same body when not |

`HealthStatusDto`: `status`, `checks[]` with `name`, `status`, `description`.

## Contracts and errors

Unknown routes: framework 404. Health does not throw to the client; failed checks become unhealthy entries.

## Related

- [overview.md](overview.md)
- [security.md](security.md)
- [../frontend/state.md](../frontend/state.md)
