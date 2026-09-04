# Job application financial and activity logs

## Context

Operations rows need a price based on how many listings on the profile main tab are Applied or Interview. Users also need a dated record of Job Application actions.

## Decision

Job Application tabs are Operations, Profiles, Financial, Statistics, and Resume generation. Gmail, financial defaults, profiles, and sources live on Settings (Job Application tab); see [030-consolidated-settings-tabs.md](030-consolidated-settings-tabs.md). Activity lives on Logs; see [031-consolidated-logs-page.md](031-consolidated-logs-page.md). Profiles is described in [032-job-application-profiles-tab.md](032-job-application-profiles-tab.md). Statistics is described in [033-job-application-statistics.md](033-job-application-statistics.md).

Financial lists one row per pipeline entry. Main (row `total` / `ready` / `notReady` / `applied` / `interviews` / `unapplied` / `price`) counts come from that profile's named main tab: total is non-empty listing rows, ready has Download filled (Status ignored), not ready has blank Download and blank Status (Status already filled — including Invalid — is not not-ready), and applied / interviews / unapplied (blank Status) count only among ready rows. Today counts the same fields among listings whose keys are in the latest Update copy batch for that profile (source listings present on main after Update: appended and skipped duplicates). Archived counts sum numbered tabs (`1`, `2`, `3`, …) created by Forward. Status dropdown maintenance skips archived tabs. Lifetime is main plus archived. Price is `applied * applyRate + interviews * bonusRate` for each period, rounded to 2 decimals. Each row has its own apply rate and bonus rate. New pipeline rows copy the user's defaults (0.06 and 1.5 until changed). Defaults are edited on Settings (Job Application), not Admin. Changing defaults does not rewrite existing row rates. Rows are selectable so a subset can be priced in the UI. Profile Status includes Interview.

`GET /api/job-application/financial` also writes an hourly snapshot for that user (`job_financial_snapshots`, unique `UserId` + UTC hour). The same request returns `history` (oldest first, up to 14 days of hours). Opening Financial or Google workspace sync updates this hour's snapshot. `GET /api/job-application/financial/history` returns the same history list. The Financial tab charts today, archived, and lifetime price with Hourly and Daily views. See [028-financial-performance-snapshots.md](028-financial-performance-snapshots.md).

Logs persist after successful pipeline, catalog, financial, and Gmail connect or disconnect actions. `GET /api/job-application/logs` returns a server-paged list with optional category and text filters.

## Consequences

Gmail is not required to open Financial or Logs. Sheet counts are zero until Gmail can read the profile workbook. Rate values are 0 to 10000 with 4 decimal places.

## Related

- [010-job-application-pipeline.md](010-job-application-pipeline.md)
- [008-job-catalog-google-sheets.md](008-job-catalog-google-sheets.md)
