# Google Cloud client in Settings

## Context

Job Application OAuth needs one Google Cloud web Client ID and secret for the Flexis deployment. Each signed-in user still connects their own Gmail. Putting those values in a committed project file blocks changing them after deploy and mixes app identity with per-user tokens.

## Decision

Admins save Client ID and Client secret on Settings Admin tab (`GET`/`PUT /api/google/client`). They live in `google_client_credentials`. The secret is AES-GCM protected. GET returns `clientId` and `hasSecret`, never the secret. OAuth reads the database first, then `Google:ClientId`/`Google:ClientSecret` if the table is empty. `Google:RedirectUri` stays in config.

Job Application create, edit, delete, Open sheet, and source location actions stay disabled until that user has connected Gmail. Connect Gmail, Copy URL, and Disconnect stay enabled.

## Consequences

Production can change the Google Cloud client without a new deploy. Each user still has a separate Gmail connection in `google_connections`. Do not put the secret in committed files.

## Related

- [006-google-oauth-job-application.md](006-google-oauth-job-application.md)
- [../backend/google-oauth-setup.md](../backend/google-oauth-setup.md)
