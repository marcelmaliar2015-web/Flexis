# Google workspace sync (Sheet refresh)

## Context

Sheet Status, listings, and Drive layout change in Google. Polling every tab independently would multiply Sheets calls. Google enforces a per-user read quota (commonly 60 reads per minute). Auto sheet refresh, manual AppBar refresh, and post-Update workspace refresh can overlap and fight each other if they are not coordinated.

## Decision

Call this process **Sheet refresh** (not pipeline Update). One signed-in `GoogleSyncProvider` plus a module-level `sheetRefreshCoordinator` own the workflow.

### Coordinator

`frontend/src/shared/api/sheetRefreshCoordinator.ts` allows only one sheet-touching refresh at a time. Kinds and priority:

1. `workspace` — after pipeline Update / Forward / related Job Application mutations (`refreshJobApplicationWorkspace`)
2. `manual` — AppBar Sheet refresh click (full refresh)
3. `auto` — timer / visibility (listing-status only)

Rules:

- Auto is skipped while anything is running, while a higher-priority job is queued, or when the last successful refresh is younger than 5 minutes.
- A new job of equal priority replaces a pending peer; a lower-priority pending job yields to a higher one.
- Running jobs are not cancelled mid-flight (avoids half-written quota storms). The next queued job starts when the current one finishes.
- Success stamps `lastSuccessAt` for every kind so the AppBar “Sheet refresh · …” clock stays honest after Update as well as after auto/manual.

### What each kind loads

- **Auto:** Financial then Statistics in sequence. Server board cache (60s) makes the Statistics call a cache hit, so Sheets is read once. No pipeline ListSheets fan-out. No banned-match scans.
- **Manual:** Google client (403 ignored), pipeline, catalogs, banned-match scans spaced 2.5s apart, Financial then Statistics (cache), logs.
- **Workspace:** Financial then Statistics (cache) and log invalidation only — enough to refresh Ready / Applied / price after Update without repeating the full manual path.

### Server

`JobFinancialService` caches the built board for 60 seconds per user and checks Status dropdown maintenance at most once per spreadsheet per 12 hours. Quota errors fail the board request instead of zeroing remaining profiles. Pipeline Update / Forward invalidate the board cache.

## Consequences

Dashboard and Statistics Today metrics stay current on the 5-minute auto tick without stacking concurrent refreshes. Manual and Update never race auto. Do not add a second timer on Financial or Statistics tabs.

## Related

- [014-header-google-status.md](014-header-google-status.md)
- [010-job-application-pipeline.md](010-job-application-pipeline.md)
- [013-job-application-financial-logs.md](013-job-application-financial-logs.md)
- [033-job-application-statistics.md](033-job-application-statistics.md)
