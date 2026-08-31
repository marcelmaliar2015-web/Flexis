# Microsoft OAuth setup (local)

One-time Azure work so Mail Check **Connect Outlook** can run. Use a Microsoft 365 or Outlook.com account that you will connect in Flexis.

The same steps are in the signed-in app at `/help` on the Microsoft setup tab.

Flexis redirect (must match the app registration exactly, no trailing slash):

`http://localhost:5080/api/mail-check/mailbox/outlook/callback`

## 1. Register an app

Open [https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade).

**New registration**. Name: `flexis-local`. Supported account types: **Accounts in any organizational directory and personal Microsoft accounts**. Redirect URI: leave blank for now. Register.

Copy **Application (client) ID** from Overview.

## 2. Client secret

Open **Certificates & secrets** → **New client secret**. Description: `flexis-local`. Add.

Copy the **Value** now. Azure does not show it again.

## 3. Redirect URI

Open **Authentication** → **Add a platform** → **Web**.

Redirect URI (exact):

- `http://localhost:5080/api/mail-check/mailbox/outlook/callback`

Do not use `127.0.0.1:5080`. Do not add a trailing slash.

Under **Implicit grant and hybrid flows**, leave access tokens and ID tokens unchecked.

Save.

## 4. API permissions

Open **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated permissions**.

Add:

| Permission | Why |
| --- | --- |
| `Mail.ReadWrite` | Read mail, move junk to inbox, trash noise |
| `MailboxSettings.ReadWrite` | Create master categories (Outlook labels) |
| `openid` | Sign-in |
| `profile` | Account identity |
| `email` | Connected address |
| `offline_access` | Refresh token |

Click **Grant admin consent** if your tenant requires it. Personal Microsoft accounts do not need tenant admin consent for these delegated scopes.

Do not add application permissions.

## 5. Put credentials in Flexis

Sign in as an admin. Open Settings. Under **Microsoft client**, paste Application (client) ID and client secret. Save.

That is one Azure app for the Flexis deployment. Each person still connects their own mailbox on Mail Check.

## 6. Confirm Flexis is running

API must be running at [http://localhost:5080/api/health](http://localhost:5080/api/health) (`Healthy`). Frontend at [http://127.0.0.1:5173/](http://127.0.0.1:5173/). Saving the Microsoft client in Settings does not need a restart.

## 7. Connect

1. [http://127.0.0.1:5173/mail-check](http://127.0.0.1:5173/mail-check) — Settings tab
2. **Connect Outlook** — sign in with the mailbox you want triaged — Accept
3. Microsoft returns to Flexis. The chip should read **Connected** and show that address.
4. Paste an OpenAI API key on the same tab before auto-check classifies mail.

Flexis creates four Outlook master categories: Interview Scheduled, Waiting for answer, Need to Schedule/Availability, and Others. See [019-mail-check.md](../decisions/019-mail-check.md).

## If Connect fails

| What you see | Fix |
| --- | --- |
| Connect Outlook stays disabled | An admin must save the Microsoft client in Settings |
| `redirect_uri_mismatch` | Redirect URI on the Azure app must be exactly `http://localhost:5080/api/mail-check/mailbox/outlook/callback` |
| `AADSTS50011` | Same as redirect mismatch; check Authentication → Web redirect URIs |
| `mailbox=error` after Microsoft | Check API logs; confirm delegated permissions in step 4 |
| `mailbox=denied` | User cancelled consent or admin blocked the app |
| Token refresh fails later | Secret expired; create a new client secret and update Settings |

## Related

- [security.md](security.md)
- [google-oauth-setup.md](google-oauth-setup.md)
- [../decisions/019-mail-check.md](../decisions/019-mail-check.md)
- [../decisions/021-microsoft-client-in-settings.md](../decisions/021-microsoft-client-in-settings.md)
