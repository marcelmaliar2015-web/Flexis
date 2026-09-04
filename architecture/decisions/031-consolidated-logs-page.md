# Consolidated Logs page

## Context

Job Application and Mail Check each had their own Log tab. Users had to switch modules to review activity, and the Job Application feed loaded a long unpaged list.

## Decision

`/logs` is a left-nav product page with MUI tabs: Job Application and Mail Check. It reuses `JobApplicationLogsTab` and `MailCheckLogTab`. Module pages no longer have Log tabs. Optional `?tab=` deep-links the active Logs tab. `GET /api/job-application/logs` returns a server-paged page (`page`, `pageSize`, optional `category` and `q`) like Mail Check logs. Page size defaults to 50 and caps at 100.

## Consequences

Help, Dashboard recent activity, and architecture point to Logs. Keep feature-owned log components under `features/jobApplication` and `features/mailCheck`; Logs only hosts them.
