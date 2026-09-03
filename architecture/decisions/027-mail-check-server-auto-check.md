# Mail Check server auto-check

## Context

Auto-check previously ran only in the browser while `/mail-check` was open and the tab was visible. Closing Chrome or switching apps stopped classification even though the API process stayed up on the VPS.

## Decision

A hosted `MailCheckAutoCheckWorker` on the API runs every 20 seconds for every user with auto-check enabled, an OpenAI key saved, and at least one mailbox connected. Each tick calls `MailCheckService.RunAsync` with `force: false` and follows `hasMore` for up to five rounds. The browser no longer drives auto-check; it only observes settings and `GET /api/mail-check/run/progress` for the AppBar status pill. Manual Check still uses the per-user server lock and waits if a server auto run is active.

## Consequences

Auto-check continues while the API process is running, independent of browser focus. Stopping `backend\run.bat` / the API stops auto-check. Frontend Mail Check copy and Help describe server-side watching.
