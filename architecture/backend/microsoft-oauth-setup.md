# Microsoft OAuth setup (local)

One-time Azure work so Mail Check **Connect Outlook** can run. Same guide is in the app at `/help` → Microsoft setup.

Do steps 1 to 8 in order. Steps 1 to 5 are Azure. Step 6 is Flexis Settings. Step 8 connects your mailbox.

Flexis redirect (exact, step 4 only):

`http://localhost:5080/api/mail-check/mailbox/outlook/callback`

For a temporary public URL via ngrok, also add the printed `{public}/api/mail-check/mailbox/outlook/callback` from [public-url-ngrok.md](public-url-ngrok.md).

## 1. Open a directory in Azure Portal

Goal: App registrations must run inside a directory. This is free Microsoft account / app setup, **not** a paid Microsoft Graph plan. Outlook mail Graph calls are not metered mail APIs.

**Skip this whole Microsoft setup if you use Gmail only.**

### Do this first (no payment setup)

1. Open [https://portal.azure.com/](https://portal.azure.com/) and sign in with the same Microsoft account you use for Outlook.
2. If the top bar already shows a directory name, go to step 2.
3. If **New registration** works on App registrations, go to step 2.
4. If New registration is blocked with “applications outside of a directory has been deprecated,” join the [Microsoft 365 Developer Program](https://developer.microsoft.com/microsoft-365/dev-program) (Contact Email, Country/Region, Company such as `Individual`, preferences, Join). No Graph payment. If **Set up E5 subscription** appears you may follow it; if you do not qualify, retry App registrations afterward.
5. If another Flexis admin already saved the Microsoft client on Settings, skip steps 1 to 6 and only Connect Outlook on Mail Check Settings.

### Only if Microsoft still blocks New registration

Some personal accounts must create a directory through Microsoft’s free Azure signup. That is still not buying Graph API access. If Microsoft’s own form asks for a card, that is their account verification. Flexis does not require it and does not charge you. Prefer finishing without that form whenever Portal already shows a directory.

1. Open [https://azure.microsoft.com/free/](https://azure.microsoft.com/free/) only if the steps above still cannot open App registrations.
2. Complete Microsoft’s free signup with your Outlook Microsoft account.
3. If Microsoft asks for a card, that is their verification form, not a Flexis or Graph fee. You can stop if you refuse; then Connect Outlook stays locked until a directory exists.
4. After signup, open Azure Portal, select the new directory, continue to step 2.

Done when: Azure Portal top bar shows a directory, or New registration opens. Then go to step 2.

## 2. Register an app

Goal: create one Azure app named `flexis-local` and copy Application (client) ID.

1. Open [App registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade).
2. Click **New registration**.
3. If Azure shows the directory-deprecated message, go back to step 1.
4. Name: `flexis-local`.
5. Supported account types: **Accounts in any organizational directory and personal Microsoft accounts**.
6. Redirect URI: leave empty.
7. Click **Register**.
8. On Overview, copy **Application (client) ID** for step 6.

Done when: you have Application (client) ID and Overview is open.

## 3. Create a client secret

Goal: create a secret Value. Azure shows it only once.

1. Stay on the same app.
2. Left menu: **Certificates & secrets**.
3. **Client secrets** → **New client secret**.
4. Description: `flexis-local`. Choose expiry. **Add**.
5. Copy the **Value** column immediately. Do not copy Secret ID.
6. Keep the Value for step 6.

Done when: you have Application (client) ID and secret Value.

## 4. Add the redirect URI

Goal: tell Azure where to send the browser after Outlook sign-in.

1. Stay on the same app.
2. Left menu: **Authentication**.
3. **Add a platform** → **Web**.
4. Paste Redirect URI exactly: `http://localhost:5080/api/mail-check/mailbox/outlook/callback`
5. No trailing slash. Do not use `127.0.0.1`.
6. Leave Implicit grant unchecked.
7. **Configure**, then **Save** if asked.

Done when: Authentication shows that Web redirect URI.

## 5. Add API permissions

Goal: allow this app to read and organize mail for the signed-in user.

1. Stay on the same app.
2. Left menu: **API permissions**.
3. **Add a permission** → **Microsoft Graph** → **Delegated permissions**.
4. Add: `User.Read`, `Mail.ReadWrite`, `MailboxSettings.ReadWrite`, `openid`, `profile`, `email`, `offline_access`.
5. **Add permissions**.
6. **Grant admin consent** if shown. Personal accounts often skip this.
7. Do not add Application permissions.

Done when: those delegated permissions are listed.

## 6. Paste credentials into Flexis

Goal: unlock Connect Outlook. This is not your mailbox yet.

1. Sign in to Flexis as an admin.
2. Open Settings → **Microsoft client**.
3. Paste Application (client) ID from step 2.
4. Paste secret Value from step 3.
5. Save.

Done when: Microsoft client is saved in Settings.

## 7. Confirm Flexis is running

1. [http://localhost:5080/api/health](http://localhost:5080/api/health) must say Healthy.
2. [http://127.0.0.1:5173/](http://127.0.0.1:5173/) must load.
3. No restart needed after step 6.

Done when: health is Healthy and the app loads.

## 8. Connect your Outlook mailbox

Goal: link the mailbox Flexis should organize.

1. Open Mail Check → Settings.
2. Click **Add Outlook**. If disabled, finish step 6.
3. Sign in with the Outlook or Microsoft 365 mailbox to triage.
4. Accept.
5. Wait for Connected and your address.
6. Paste an OpenAI API key, pick a model, Save.

Done when: mailbox Connected and OpenAI key saved.

## If something fails

| What you see | Fix |
| --- | --- |
| New registration blocked / outside a directory deprecated | Finish step 1, then retry step 2 |
| Connect Outlook / Add Outlook stays disabled | Finish step 6 |
| `redirect_uri_mismatch` or `AADSTS50011` | Step 4 URI must match exactly |
| `mailbox=error` | Check API logs; recheck step 5 |
| `mailbox=denied` | User cancelled consent |
| Token refresh fails later | New client secret in Azure, update Settings |

## Related

- [security.md](security.md)
- [google-oauth-setup.md](google-oauth-setup.md)
- [../decisions/019-mail-check.md](../decisions/019-mail-check.md)
- [../decisions/021-microsoft-client-in-settings.md](../decisions/021-microsoft-client-in-settings.md)
