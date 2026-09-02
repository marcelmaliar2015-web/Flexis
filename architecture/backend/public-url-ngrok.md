# Public URL with ngrok

Expose the local Flexis UI (and proxied API) on an HTTPS public URL for demos or OAuth callbacks from the internet.

## How it works

One ngrok tunnel targets the Vite frontend on `127.0.0.1:5173`. Browser calls to `/api` go through the Vite proxy to the API on `127.0.0.1:5080`. OAuth redirect URIs use the same public origin so Google and Microsoft callbacks hit the tunnel.

## Prerequisites

- `backend\run.bat` and `frontend\run.bat` already running
- [ngrok](https://ngrok.com/download) on PATH with an authtoken (`ngrok config add-authtoken ...`)
- Vite `allowedHosts` includes ngrok domains (see `frontend/vite.config.ts`)

## Commands

From the repo root:

```bat
run-public.bat
```

Stop the tunnel and restore localhost redirect URIs in the local settings override:

```bat
stop-public.bat
```

Scripts live under `tools/ngrok/`.

## What `run-public.bat` writes

Creates or updates `backend/src/Flexis.Api/appsettings.Development.local.json` (gitignored):

- `Frontend:Origins` — adds the public HTTPS origin; keeps localhost origins
- `Google:RedirectUri` — `{public}/api/google/connections/callback`
- `Microsoft:RedirectUri` — `{public}/api/mail-check/mailbox/outlook/callback`

Restart the API after starting or stopping the public tunnel so CORS and OAuth redirect options reload.

## OAuth consoles

Add the printed public redirect URIs in Google Cloud and Azure (keep the localhost URIs). Free ngrok URLs change when the tunnel restarts; update the consoles and re-run `run-public.bat` each time.

## Related

- [google-oauth-setup.md](google-oauth-setup.md)
- [microsoft-oauth-setup.md](microsoft-oauth-setup.md)
- [../decisions/025-ngrok-public-url.md](../decisions/025-ngrok-public-url.md)
