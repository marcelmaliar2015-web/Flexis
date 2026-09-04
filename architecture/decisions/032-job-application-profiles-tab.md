# Job Application Profiles tab

## Context

Pipeline Operations and Financial already pair profiles with sources and rates, but there was no workspace table focused on profile detail and apply status. Users needed a clickable profile list and a dedicated profile page.

## Decision

Job Application adds a Profiles tab. Rows are pipeline entries shown with profile name, source, profile sheet URL, apply rate, and bonus rate. Row click opens `/job-application/profiles/:entryId`. That page shows sheet link, editable rates, apply status (today / archived / lifetime Applied, Interview, listing totals, and price), profile info, and banned companies. `JobFinancialRowDto` includes `profileId` and `profileUrl` so the table does not join catalogs on the client.

## Consequences

Operations remains the place for Update, Forward, and pairing edits. Profiles is the read-and-detail surface for profile financial and catalog context. Help and architecture list Profiles beside Operations and Financial.
