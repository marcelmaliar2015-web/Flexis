# Frontend state

## Client state

None yet. No global client store.

## Server data

TanStack Query. Query client is created in `frontend/src/app/providers/queryClient.ts`. Feature hooks live next to the screen (`useHealthStatus`). `HomePage` also reads `healthQueryKey` from `shared/api/health` so the landing preview shares the same cache.

## Data flow

Screen hook → `shared/api` function → `GET {VITE_API_BASE_URL}{path}` → JSON DTO. In Development `VITE_API_BASE_URL` is empty, so the browser calls `/api/...` on the Vite host and Vite proxies to `http://127.0.0.1:5080`. If the API is down, `getJson` throws `API is not running. Start backend/src/Flexis.Api.`

## Related

- [overview.md](overview.md)
- [../backend/api.md](../backend/api.md)
