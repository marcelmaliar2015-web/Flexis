# Ngrok public URL

## Context

Local Flexis runs on Vite `127.0.0.1:5173` with the API on `localhost:5080`. Demos and some OAuth flows need a single public HTTPS origin.

## Decision

Expose only the frontend port through ngrok. Route API traffic as `/api` on that same public origin via the Vite proxy. Write public `Frontend:Origins` and OAuth `RedirectUri` values into gitignored `appsettings.Development.local.json`. Provide `run-public.bat` / `stop-public.bat` at the repo root.

## Consequences

- One tunnel and one public origin for UI and OAuth callbacks
- API and Vite must already be running before starting ngrok
- API restart is required after local settings change
- Google and Azure redirect allowlists must include each temporary ngrok URL
