# Job application pipeline

## Context

Operations needs one table to send listings from a source location tab into a profile sheet. Repeating that must not duplicate rows already on the profile. After a cycle of listings, the filled profile tab must become a locked log and a new empty main tab must take the original name. Each pipeline row also needs its own screen for edits, actions, and banned companies.

## Decision

Operations is a Pipeline table. Each entry pairs a profile with a source location (`Source title · location`). The same profile and source location cannot be added twice. A row click opens `/job-application/pipeline/{id}` for profile and source edits, Update, Forward, Delete, and banned companies. Update All and Forward All stay on the table.

Update copies Company Name, Position, Link, and JD from that source tab onto the profile **main** tab and skips rows already present or banned. Forward archives the main tab as the next unused positive integer and creates a new empty main tab with the original name. Forward All runs once per distinct profile.

After Update or Forward, Flexis reapplies protection. On the named main tab the owner can edit every cell; invited editors can edit only Status and Issue. Numbered log tabs are owner-only.

## Consequences

Gmail must be connected. Pipeline entries live in `job_pipeline_entries`. Deleting an entry does not remove listings already written to the profile sheet. Update always writes to the named main tab, not a log tab. Banned company matching is in [012-pipeline-banned-companies.md](012-pipeline-banned-companies.md).

## Related

- [008-job-catalog-google-sheets.md](008-job-catalog-google-sheets.md)
- [012-pipeline-banned-companies.md](012-pipeline-banned-companies.md)
