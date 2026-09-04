# Job Application Statistics tab

## Context

Financial showed current sheet prices and a user-level hourly chart. Users still could not see applies and prices per profile across hour, day, week, and month, or how many listings still had a blank Status. Treating Today as the full main sheet and period charts as end-of-period stock was wrong for volume reporting.

## Decision

Job Application adds a Statistics tab. `JobFinancialRules.CountStatuses` returns `Unapplied` for non-empty listing rows with a blank Status, optionally filtered to listing keys from the latest Update copy batch.

Today is listings from the last pipeline Update for that profile (`job_listing_copy_batches` / `job_listing_copy_items`): every non-banned source listing from that Update (newly appended and already-present skipped duplicates). Each Update replaces the profile batch with those source keys only. Today stays empty until Update runs. **Main / Total** is the full profile main sheet. A listing is **ready** when Download is filled; **not ready** when Download is blank. Applied, Interview, Unapplied (blank Status), and price count only among ready rows. Opening Financial or Statistics syncs Status into `job_listing_status_states` and records transitions into Applied or Interview on `job_listing_status_events` with `OccurredAt`.

`GET /api/job-application/financial/statistics` returns per-profile main and today counts (including ready/not ready) plus history points built from status events (Applied/Interview volumes per UTC hour, priced with the profile rates). Unapplied on history points uses the last-update cohort stock from `job_profile_statistics_snapshots`. The UI toggles Today, Hourly, Daily, Weekly, and Monthly; period views chart Applied, Interview, and Price together (summed for All profiles, or one profile when filtered).

## Consequences

Today is empty until the first Update for that profile. Period Applied/Price history starts after Status changes are observed (baseline on first read does not emit events). Blank Status is unapplied only on ready rows; Expired, Banned, Invalid, and Other are not.
