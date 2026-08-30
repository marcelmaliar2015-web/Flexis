# Frontend state

## Client state

Session in `shared/auth/AuthProvider.tsx`. Access token in memory plus `localStorage` key `flexis.accessToken`. No other global client store.

## Server data

TanStack Query. Query client is created in `frontend/src/app/providers/queryClient.ts`. Feature hooks live next to the screen (`useHealthStatus`). `HomePage` also reads `healthQueryKey` from `shared/api/health` so the landing preview shares the same cache. Users list uses `usersQueryKey`. Google connection uses `googleConnectionQueryKey`. Google Cloud client uses `googleClientQueryKey`. Job catalog lists use `jobCatalogQueryKey`. Source locations use `sourceLocationsQueryKey`.

## Data flow

Screen hook or auth provider → `shared/api` function → `GET/POST/PUT/DELETE {VITE_API_BASE_URL}{path}` → JSON DTO (DELETE may be `204`). The client attaches `Authorization: Bearer` when a token is set. In Development `VITE_API_BASE_URL` is empty, so the browser calls `/api/...` on the Vite host and Vite proxies to `http://127.0.0.1:5080`. If the API is down, `getJson` throws `API is not running. Start backend/src/Flexis.Api.` Failed API payloads (except health `503`) throw `ApiError` with the Problem Details `detail`. Gmail connect redirects the browser to Google, then the API callback redirects back to `/job-application`.

## Related

- [overview.md](overview.md)
- [../backend/api.md](../backend/api.md)
