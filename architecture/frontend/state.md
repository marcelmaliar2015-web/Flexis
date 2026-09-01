# Frontend state

## Client state

Session in `shared/auth/AuthProvider.tsx`. Access token in memory plus `localStorage` key `flexis.accessToken`. `replaceUser` updates the signed-in `UserDto` after `PUT /api/auth/me`. Issue notices live in `shared/notifications/issueStore.ts` and `localStorage` key `flexis.issueNotices` (newest 50). No other global client store.

## Server data

TanStack Query. Query client is created in `frontend/src/app/providers/queryClient.ts`. Feature hooks live next to the screen (`useHealthStatus`). `HomePage` also reads `healthQueryKey` from `shared/api/health` so the landing preview shares the same cache. Users list uses `usersQueryKey`. Google connection uses `googleConnectionQueryKey` (Job Application Gmail card, signed-in AppBar Gmail status, and AppBar Google client readiness). Google Cloud client uses `googleClientQueryKey`. Job catalog lists use `jobCatalogQueryKey`. Profile info uses `profileInfoQueryKey`. Source locations use `sourceLocationsQueryKey`. Pipeline uses `jobPipelineQueryKey`. Banned companies use `jobPipelineBannedQueryKey`. Sheet matches use `jobPipelineBannedMatchesQueryKey` and refetch every 10 seconds on the pipeline entry page. Financial uses `jobFinancialQueryKey`. Activity logs use `jobApplicationLogsQueryKey`. Mail Check uses `mailCheckSettingsQueryKey`, `mailCheckMailboxQueryKey` (mailbox list plus AppBar Microsoft client readiness via `outlookAvailable`), `mailCheckModelsQueryKey`, `mailCheckInboxQueryKey`, and `mailCheckLastRunQueryKey`. Inbox refetches every 30 seconds while that tab is mounted. Dashboard reads health, Google connection, pipeline, financial, logs, and `usersQueryKey` when Admin. Signed-in `GoogleSyncProvider` refreshes Google-backed keys every 3 minutes (connection, then pipeline and financial when Gmail is connected) and on AppBar click (full sheet and catalog refresh). Signed-in `MailCheckProvider` in `AuthenticatedLayout` runs a Mail Check pass about every 2 minutes on `/mail-check` while the browser tab is visible when a mailbox is connected and an OpenAI key is saved. Failed API calls, Mail Check item failures, unhandled browser errors, and UI render crashes write issue notices. See [../decisions/015-google-workspace-sync.md](../decisions/015-google-workspace-sync.md), [../decisions/019-mail-check.md](../decisions/019-mail-check.md), and [../decisions/020-issue-notifications.md](../decisions/020-issue-notifications.md). After a full page refresh the query cache is empty. Screens use `isQueryLoading` from `shared/api/queryState.ts` so they show Loading instead of treating undefined data as zero or an empty list.

## Data flow

Screen hook or auth provider → `shared/api` function → `GET/POST/PUT/DELETE {VITE_API_BASE_URL}{path}` → JSON DTO (DELETE may be `204`). The client attaches `Authorization: Bearer` when a token is set. In Development `VITE_API_BASE_URL` is empty, so the browser calls `/api/...` on the Vite host and Vite proxies to `http://127.0.0.1:5080`. If the API is down, `getJson` throws `API is not running. Start backend/src/Flexis.Api.` Failed API payloads (except health `503`) throw `ApiError` with the Problem Details `detail`. Gmail connect redirects the browser to Google, then the API callback redirects back to `/job-application`.

## Related

- [overview.md](overview.md)
- [../backend/api.md](../backend/api.md)
