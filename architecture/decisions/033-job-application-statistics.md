# Job Application Statistics tab

## Context

Financial showed current sheet prices and a user-level hourly chart. Users still could not see applies and prices per profile across hour, day, week, and month, or how many listings still had a blank Status.

## Decision

Job Application adds a Statistics tab. `JobFinancialRules.CountStatuses` returns `Unapplied` for non-empty listing rows with a blank Status. `GET /api/job-application/financial/statistics` returns live per-profile today counts (deduped by profile) plus about 93 days of hourly `job_profile_statistics_snapshots`. Opening Statistics or Financial captures the current UTC hour for each profile. The UI toggles Today, Hourly, Daily, Weekly, and Monthly; charts Applied, Price, or Unapplied; and filters by profile.

## Consequences

History starts when Statistics or Financial loads with Gmail readable. Blank Status is unapplied; Expired, Banned, Invalid, and Other are not. Period views use end-of-period stock per profile, not event deltas.
