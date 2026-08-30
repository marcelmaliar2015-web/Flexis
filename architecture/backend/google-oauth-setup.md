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

Add these (search, check, update):

- `https://www.googleapis.com/auth/gmail.modify`
- `https://www.googleapis.com/auth/spreadsheets`
- `https://www.googleapis.com/auth/drive.file`
- `https://www.googleapis.com/auth/userinfo.email`
- `openid`

Save. Google may warn that Gmail is restricted. That is expected for local Testing.

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

Edit `backend/src/Flexis.Api/appsettings.Development.json`. Set `Google:ClientId` and `Google:ClientSecret`. Leave `RedirectUri` as `http://localhost:5080/api/google/connections/callback`.

Do not commit the secret. PowerShell alternative (no file edit):

```powershell
$env:Google__ClientId = "PASTE_CLIENT_ID"
$env:Google__ClientSecret = "PASTE_CLIENT_SECRET"
```

Then start the API in that same terminal.

## 8. Restart the API

API must be running at [http://localhost:5080/api/health](http://localhost:5080/api/health) (`Healthy`). Frontend at [http://127.0.0.1:5173/](http://127.0.0.1:5173/).

If you only edited JSON, stop and start `dotnet run --project backend/src/Flexis.Api --launch-profile http`.

## 9. Connect

1. [http://127.0.0.1:5173/sign-in](http://127.0.0.1:5173/sign-in) — `admin@flexis.local` / `FlexisAdmin1!`
2. [http://127.0.0.1:5173/job-application](http://127.0.0.1:5173/job-application) — Settings tab
3. **Connect Gmail** — pick the test-user Gmail — Allow
4. Google sends the browser to `http://localhost:5080/api/google/connections/callback`, then Flexis returns to `http://127.0.0.1:5173/job-application?google=connected`

The chip should read **Connected** and show that Gmail address.

## If Connect fails

| What you see | Fix |
| --- | --- |
| Connect Gmail stays disabled | ClientId or ClientSecret empty; restart API after setting them |
| `redirect_uri_mismatch` | Redirect URI on the client must be exactly `http://localhost:5080/api/google/connections/callback` |
| Access blocked / app not verified | Add that Gmail under Test users; stay on Testing |
| 403 API not enabled | Repeat step 2 on the same project as the client |
| `google=error` after Google | Check API logs; confirm scopes in step 5 |

## Related

- [security.md](security.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
