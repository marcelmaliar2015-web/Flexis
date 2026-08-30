# Google OAuth for Job Application

## Context

Job Application needs Gmail plus Google Sheets and Drive. Tokens must not live in the browser. Access must stay least-privilege.

## Decision

Use Google OAuth 2.0 authorization code flow with PKCE, `access_type=offline`, and `prompt=consent`. The API holds the client secret, exchanges the code, and stores AES-GCM protected refresh tokens per Flexis user in PostgreSQL.

Scopes: `openid`, `userinfo.email`, `gmail.modify`, `spreadsheets`, and `drive.file`. Gmail covers job mail. Sheets covers create, edit, and delete. `drive.file` covers upload, download, and delete for files Flexis creates or the user opens with Flexis, not the rest of Drive.

Connect and disconnect live on Job Application. The Google callback is anonymous; CSRF is a one-time server state bound to the Flexis user.

## Consequences

Later Sheets and Drive calls reuse the stored refresh token. Do not add a Google client secret to the frontend. Do not request `drive` or `mail.google.com`. Production needs a Google Cloud OAuth web client, those APIs enabled, and Google verification for Gmail.
