# Financial performance snapshots

## Context

Financial only showed current sheet counts. A daily-only series made the chart look empty when only one day existed, and users need hourly tracking with an optional daily view.

## Decision

PostgreSQL table `job_financial_snapshots` stores one row per user per UTC hour (`CapturedHour`, unique with `UserId`). `CapturedOn` is the UTC date of that hour. Columns cover today (last Update batch), main sheet, archived, and lifetime. `GET /api/job-application/financial` upserts the current hour's totals (unique-constraint safe under concurrent Financial/Statistics loads) and returns `history` (up to 14 days of hourly points, oldest first). `GET /api/job-application/financial/history` returns the same list. The Financial tab chart toggles Hourly (raw points) and Daily (last snapshot per day), draws connected lines with markers, grid, and hover detail for Today, Main, Archived, and Lifetime.

## Consequences

History starts the first time Financial or Google sync loads the board. Earlier hours are not backfilled. Sheet counts in a snapshot stay zero when Gmail cannot read workbooks at capture time.
