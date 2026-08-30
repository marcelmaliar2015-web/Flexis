# Google OAuth setup (local)

One-time Google Cloud work so Job Application **Connect Gmail** can run. Billing is not required. Use a Google account that you will connect in Flexis.

The same steps are in the signed-in app at `/help`.

Flexis redirect (must match the client exactly, no trailing slash):

`http://localhost:5080/api/google/connections/callback`

After setup, sign in at `http://127.0.0.1:5173/sign-in` (`admin@flexis.local` / `FlexisAdmin1!`) and open `http://127.0.0.1:5173/job-application`.

## 1. Create a project

Open [https://console.cloud.google.com/projectcreate](https://console.cloud.google.com/projectcreate).

Name it `flexis-local`. Create. Skip billing if asked. Confirm the top bar shows this project.

## 2. Enable APIs

Open each URL, then click **Enable**:

- Gmail: [https://console.cloud.google.com/apis/library/gmail.googleapis.com](https://console.cloud.google.com/apis/library/gmail.googleapis.com)
- Sheets: [https://console.cloud.google.com/apis/library/sheets.googleapis.com](https://console.cloud.google.com/apis/library/sheets.googleapis.com)
- Drive: [https://console.cloud.google.com/apis/library/drive.googleapis.com](https://console.cloud.google.com/apis/library/drive.googleapis.com)

If the page says **Manage**, it is already on.

## 3. Branding

Open [https://console.cloud.google.com/auth/overview](https://console.cloud.google.com/auth/overview).

If you see **Get started**, click it. App name `Flexis`. User support email: your Gmail. Audience: **External**. Developer contact: your Gmail. Finish.

If branding already exists, open [https://console.cloud.google.com/auth/branding](https://console.cloud.google.com/auth/branding) and set the same App name.

## 4. Test user

Open [https://console.cloud.google.com/auth/audience](https://console.cloud.google.com/auth/audience).

Publishing status must stay **Testing**. Under **Test users**, add the Gmail you will use on **Connect Gmail**. Save.

Google blocks other accounts while the app is in Testing.

## 5. Scopes

Open [https://console.cloud.google.com/auth/scopes](https://console.cloud.google.com/auth/scopes).

The picker does **not** list the raw URLs as options. Finish step 2 first. Only enabled APIs show scopes.

Click **Add or remove scopes**. Then either:

- Search the table for **Gmail API**, **Google Sheets API**, or **Google Drive API**, and check the console name below.
- Or scroll to **Manually add scopes**, paste each URI, click **Add to table**, then **Update**.

| Console name | URI to paste | Kind |
| --- | --- | --- |
| Read, compose, and send emails from your Gmail account | `https://www.googleapis.com/auth/gmail.modify` | Restricted |
| See, edit, create, and delete all your Google Sheets spreadsheets | `https://www.googleapis.com/auth/spreadsheets` | Sensitive |
| See, edit, create, and delete only the specific Google Drive files you use with this app | `https://www.googleapis.com/auth/drive.file` | Sensitive |
| See your primary Google Account email address | `https://www.googleapis.com/auth/userinfo.email` | Non-sensitive |
| openid | `openid` | Non-sensitive |

Gmail modify is restricted. That is expected for local Testing. Do not pick See and download all your Gmail (`mail.google.com`) or See, edit, create, and delete all of your Google Drive files (`drive`).

## 6. Web client

Open [https://console.cloud.google.com/auth/clients](https://console.cloud.google.com/auth/clients).

**Create client**. Application type: **Web application**. Name: `flexis-local-web`.

Authorized JavaScript origins (Add URI twice):

- `http://localhost:5173`
- `http://127.0.0.1:5173`

Authorized redirect URIs (Add URI once, exact):

- `http://localhost:5080/api/google/connections/callback`

**Create**. Copy **Client ID** and **Client secret** now.

Do not use Desktop, Android, or iOS. Do not use `127.0.0.1:5080` for the redirect.

## 7. Put credentials in Flexis

Sign in as an admin. Open Settings. Under **Google Cloud client**, paste Client ID and Client secret. Save.

That is one client for the Flexis app. Each person still connects their own Gmail on Job Application. Do not put the secret in a committed project file.

Redirect URI stays `http://localhost:5080/api/google/connections/callback` in `appsettings.Development.json`.

## 8. Confirm Flexis is running

API must be running at [http://localhost:5080/api/health](http://localhost:5080/api/health) (`Healthy`). Frontend at [http://127.0.0.1:5173/](http://127.0.0.1:5173/). Saving the Google Cloud client in Settings does not need a restart.

## 9. Connect

1. [http://127.0.0.1:5173/sign-in](http://127.0.0.1:5173/sign-in) — `admin@flexis.local` / `FlexisAdmin1!`
2. [http://127.0.0.1:5173/job-application](http://127.0.0.1:5173/job-application) — Settings tab
3. **Connect Gmail** — pick the test-user Gmail — Allow
4. Google sends the browser to `http://localhost:5080/api/google/connections/callback`, then Flexis returns to `http://127.0.0.1:5173/job-application?google=connected`

The chip should read **Connected** and show that Gmail address.

Flexis then creates `Flexis` / `Job Application` / `Profiles` and `Sources` in that Google Drive and keeps Job Application spreadsheets there. See [011-google-drive-folder-layout.md](../decisions/011-google-drive-folder-layout.md).

## If Connect fails

| What you see | Fix |
| --- | --- |
| `gmail.modify` or `spreadsheets` not in the list | Enable the APIs in step 2, then **Manually add scopes** and paste the URIs |
| Connect Gmail stays disabled | An admin must save the Google Cloud client in Settings |
| `redirect_uri_mismatch` | Redirect URI on the client must be exactly `http://localhost:5080/api/google/connections/callback` |
| Access blocked / app not verified | Add that Gmail under Test users; stay on Testing |
| 403 API not enabled | Repeat step 2 on the same project as the client |
| `google=error` after Google | Check API logs; confirm scopes in step 5 |

## Related

- [security.md](security.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
- [../decisions/009-google-client-in-settings.md](../decisions/009-google-client-in-settings.md)
- [../decisions/011-google-drive-folder-layout.md](../decisions/011-google-drive-folder-layout.md)
