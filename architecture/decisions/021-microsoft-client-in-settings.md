# Microsoft client in Settings

## Context

Mail Check Outlook OAuth needs one Azure app registration Client ID and secret for the Flexis deployment. Each signed-in user still connects their own mailbox. Putting those values only in committed config blocks changing them after deploy.

## Decision

Admins save Application (client) ID and client secret on Settings (`GET`/`PUT /api/microsoft/client`). They live in `microsoft_client_credentials`. The secret is AES-GCM protected with `Google:TokenProtectionKey`. GET returns `clientId` and `hasSecret`, never the secret. OAuth reads the database first, then `Microsoft:ClientId`/`Microsoft:ClientSecret` if the table is empty. `Microsoft:RedirectUri` and `Microsoft:TenantId` stay in config.

## Consequences

Production can change the Azure app without a new deploy. Each user may connect multiple mailboxes in `mail_connections`. Do not put the secret in committed files.

## Related

- [009-google-client-in-settings.md](009-google-client-in-settings.md)
- [019-mail-check.md](019-mail-check.md)
- [../backend/microsoft-oauth-setup.md](../backend/microsoft-oauth-setup.md)
