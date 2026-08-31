# Job application financial and activity logs

## Context

Operations rows need a price based on how many listings on the profile main tab are Applied or Interview. Users also need a dated record of Job Application actions.

## Decision

Job Application tabs are Operations, Financial, Logs, then Settings.

Financial lists one row per pipeline entry. Counts come from that profile's named main tab: total is non-empty listing rows, applied is Status `Applied`, interviews is Status `Interview`. Price is `applied * applyRate + interviews * bonusRate`, rounded to 2 decimals. Each row has its own apply rate and bonus rate. New pipeline rows copy the user's defaults (0.06 and 1.5 until changed). Defaults are edited on Job Application Settings, not Admin Settings. Changing defaults does not rewrite existing row rates. Rows are selectable so a subset can be priced in the UI. Profile Status includes Interview.

Logs persist after successful pipeline, catalog, financial, and Gmail connect or disconnect actions. `GET /api/job-application/logs` returns the newest 200 for that user.

## Consequences

Gmail is not required to open Financial or Logs. Sheet counts are zero until Gmail can read the profile workbook. Rate values are 0 to 10000 with 4 decimal places.

## Related

- [010-job-application-pipeline.md](010-job-application-pipeline.md)
- [008-job-catalog-google-sheets.md](008-job-catalog-google-sheets.md)
