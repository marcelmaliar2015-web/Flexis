# Job application pipeline

## Context

Operations needs one table to send listings from a source location tab into a profile sheet. Repeating that must not duplicate rows already on the profile. After a cycle of listings, the filled profile tab must become a locked log and a new empty main tab must take the original name. Each pipeline row also needs its own screen for edits, actions, profile info, and that profile's banned companies.

## Decision

Operations is a Pipeline table. Each entry pairs a profile with a source location (`Source title · location`). The same profile and source location cannot be added twice. A row click opens `/job-application/pipeline/{id}` for profile and source edits, profile info, Update, Forward, Delete, and that profile's banned companies. Update All, Forward All, and Delete All stay on the table.

Update copies Company Name, Position, Link, and JD from that source tab onto the profile **main** tab immediately after the last filled listing row and skips rows already present or banned. Before appending, Update removes blank rows between filled listings and rewrites the block as a contiguous table. When the append would pass the sheet grid, Flexis expands row and column capacity first, then grows the listings table range. Forward archives the main tab as the next unused positive integer and creates a new empty main tab with the original name. Forward All runs once per distinct profile.

After Update, Flexis resizes the listings table to the real data and reapplies protection. On the named main tab the owner can edit every cell. Invited editors can edit Status and Issue only on rows whose listing still appears in the current source; rows no longer in the source are fully locked for invited editors. Numbered log tabs are owner-only. After Forward, Flexis reapplies the usual column Status/Issue unlock on the new empty main tab.

## Consequences

Gmail must be connected. Pipeline entries live in `job_pipeline_entries`. Deleting an entry does not remove listings already written to the profile sheet. Update always writes to the named main tab, not a log tab. `GET /api/job-application/pipeline` still returns stored entries when Google credentials fail; source location tabs are empty until Gmail can read sheets again. Banned company matching is in [012-pipeline-banned-companies.md](012-pipeline-banned-companies.md).

## Related

- [008-job-catalog-google-sheets.md](008-job-catalog-google-sheets.md)
- [012-pipeline-banned-companies.md](012-pipeline-banned-companies.md)
- [013-job-application-financial-logs.md](013-job-application-financial-logs.md)
