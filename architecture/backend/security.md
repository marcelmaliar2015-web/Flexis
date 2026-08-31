# Backend security

## Authentication

JWT Bearer. Sign-in `POST /api/auth/sign-in` returns `accessToken` and `UserDto`. `GET /api/auth/me` returns the current user. `PUT /api/auth/me` updates display name and optional password for that user. Tokens are signed with `Jwt:SigningKey` (issuer `Jwt:Issuer`, audience `Jwt:Audience`). Development seed (empty `users` table): `admin@flexis.local` / `FlexisAdmin1!`, `user@flexis.local` / `FlexisUser1!`, `viewer@flexis.local` / `FlexisViewer1!`.

## Authorization

Fallback policy requires an authenticated user. `GET /api/health`, `POST /api/auth/sign-in`, `GET /api/google/connections/callback`, and `GET /api/mail-check/mailbox/outlook/callback` are anonymous. `GET/POST /api/users` and `PUT/DELETE /api/users/{id}` require role `Admin`. `PUT /api/auth/me` is authenticated and cannot change role or active. `GET/PUT /api/google/client` require role `Admin`. `POST /api/diagnostics/events` is authenticated. Roles: `Admin`, `User`, `Viewer`. CORS policy `Frontend` allows `Frontend:Origins`.

Job Application Google connect uses authorization code + PKCE. Refresh tokens are AES-GCM protected with `Google:TokenProtectionKey`. Scopes are listed in [006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md). Local Google Cloud steps: [google-oauth-setup.md](google-oauth-setup.md). The same guide is in the app at `/help` on the Google setup tab. Mail Check uses a separate mailbox OAuth connection in `mail_connections`. Gmail scopes are in [019-mail-check.md](../decisions/019-mail-check.md). Outlook uses Microsoft Graph mail scopes configured in `Microsoft` appsettings.

## Secrets and transport

Development uses HTTP on port 5080, local Compose credentials, and a development JWT key in `appsettings.Development.json`. The Flexis Google Cloud Client ID and secret are saved by an admin in Settings and stored AES-GCM protected. `Google:RedirectUri` and `Google:TokenProtectionKey` stay in config. `Microsoft:ClientId`, `Microsoft:ClientSecret`, and `Microsoft:RedirectUri` configure Outlook mail connect. The per-user OpenAI API key is AES-GCM protected with the same `Google:TokenProtectionKey` and is never returned.

## Related

- [overview.md](overview.md)
- [api.md](api.md)
- [../frontend/routing.md](../frontend/routing.md)
- [../decisions/004-jwt-role-users.md](../decisions/004-jwt-role-users.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
- [../decisions/009-google-client-in-settings.md](../decisions/009-google-client-in-settings.md)
- [../decisions/017-signed-in-account-profile.md](../decisions/017-signed-in-account-profile.md)
- [../decisions/019-mail-check.md](../decisions/019-mail-check.md)
- [../decisions/020-issue-notifications.md](../decisions/020-issue-notifications.md)
- [google-oauth-setup.md](google-oauth-setup.md)
