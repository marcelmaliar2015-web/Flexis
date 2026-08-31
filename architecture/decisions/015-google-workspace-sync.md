# Google workspace sync

## Context

Sheet Status, listings, and Drive layout change in Google. Polling every tab independently would multiply Sheets calls. The signed-in user is often a Google test user, and a workspace can hold many profile and source workbooks.

## Decision

One signed-in `GoogleSyncProvider` drives Google-backed cache. Auto sync runs every 3 minutes while the tab is visible, and when the tab becomes visible if the last run is older than 3 minutes. Auto loads connection status, then if Gmail is connected the pipeline board (Drive layout and sheet maintenance) and the financial board (profile listing counts), one after the other. Clicking the AppBar freshness control runs a full sync: Google client (ignored on 403), pipeline, profiles, sources, cached locations and banned queries, financial, and logs.

The control sits left of Gmail status. It shows `Updated x mins ago` (hidden on `sm` down) and a three-lamp bar: red stale, amber aging, green fresh. Lamps chase while a sync is running. Green is under 2 minutes; amber under 8; red after that or on failure.

## Consequences

Do not add a second timer on Financial or Operations. Banned-match polling on the pipeline entry page stays at 10 seconds for that screen only. Connect and disconnect stay on Job Application Settings.

## Related

- [014-header-google-status.md](014-header-google-status.md)
- [010-job-application-pipeline.md](010-job-application-pipeline.md)
- [013-job-application-financial-logs.md](013-job-application-financial-logs.md)
