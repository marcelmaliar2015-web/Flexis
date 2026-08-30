# Backend security

## Authentication

JWT Bearer. Sign-in `POST /api/auth/sign-in` returns `accessToken` and `UserDto`. `GET /api/auth/me` returns the current user. Tokens are signed with `Jwt:SigningKey` (issuer `Jwt:Issuer`, audience `Jwt:Audience`). Development seed (empty `users` table): `admin@flexis.local` / `FlexisAdmin1!`, `user@flexis.local` / `FlexisUser1!`, `viewer@flexis.local` / `FlexisViewer1!`.

## Authorization

Fallback policy requires an authenticated user. `GET /api/health`, `POST /api/auth/sign-in`, and `GET /api/google/connections/callback` are anonymous. `GET/POST /api/users` and `PUT /api/users/{id}` require role `Admin`. Roles: `Admin`, `User`, `Viewer`. CORS policy `Frontend` allows `Frontend:Origins`.

Job Application Google connect uses authorization code + PKCE. Refresh tokens are AES-GCM protected with `Google:TokenProtectionKey`. Scopes are listed in [006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md).

## Secrets and transport

Development uses HTTP on port 5080, local Compose credentials, and a development JWT key in `appsettings.Development.json`. Production secrets must come from environment or a secret store, not committed files.

## Related

- [overview.md](overview.md)
- [api.md](api.md)
- [../frontend/routing.md](../frontend/routing.md)
- [../decisions/004-jwt-role-users.md](../decisions/004-jwt-role-users.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
