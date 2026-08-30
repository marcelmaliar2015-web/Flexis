# Backend API

## Style

ASP.NET Core controllers. JSON camelCase. Enums as strings. Route prefix `api/`. CORS policy `Frontend` allows `Frontend:Origins`. OpenAPI mapped in Development at `/openapi/v1.json` (anonymous). Application exceptions map to Problem Details (`401`, `400`, `404`, `409`, `500`).

## Endpoints

| Method | Path | Access | Response |
| --- | --- | --- | --- |
| GET | `/api/health` | Anonymous | `200` or `503` + `HealthStatusDto` |
| POST | `/api/auth/sign-in` | Anonymous | `200` + `SignInResultDto` |
| GET | `/api/auth/me` | Authenticated | `200` + `UserDto` |
| GET | `/api/users` | Admin | `200` + `UserDto[]` |
| POST | `/api/users` | Admin | `201` + `UserDto` |
| PUT | `/api/users/{id}` | Admin | `200` + `UserDto` |
| GET | `/api/google/connections` | Authenticated | `200` + `GoogleConnectionStatusDto` |
| POST | `/api/google/connections/start` | Authenticated | `200` + `GoogleConnectStartDto` |
| GET | `/api/google/connections/callback` | Anonymous | `302` to the Job Application return URL |
| DELETE | `/api/google/connections` | Authenticated | `204` |

`UserDto`: `id`, `email`, `displayName`, `role`, `isActive`, `createdAt`. Role values: `Admin`, `User`, `Viewer`.

`GoogleConnectionStatusDto`: `configured`, `connected`, `googleEmail`, `connectedAt`, `capabilities`. `GoogleConnectStartDto`: `authorizationUrl`. Start body: `returnUrl` (origin must be in `Frontend:Origins`, path `/job-application`).

## Contracts and errors

Unknown routes: framework 404. Invalid sign-in: `401`. Duplicate email: `409`. Last active admin cannot be demoted or deactivated: `409`. Google start with a missing client or a bad return URL: `400`. Health does not throw to the client; failed checks become unhealthy entries.

## Related

- [overview.md](overview.md)
- [security.md](security.md)
- [../frontend/state.md](../frontend/state.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
