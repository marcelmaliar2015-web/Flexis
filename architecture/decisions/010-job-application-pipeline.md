# Job application pipeline

## Context

Operations needs one table to send listings from a source location tab into a profile sheet. Repeating that must not duplicate rows already on the profile. After a cycle of listings, the filled profile tab must become a locked log and a new empty main tab must take the original name.

## Decision

Operations is a Pipeline table. Each entry pairs a profile with a source location (`Source title · location`). The same profile and source location cannot be added twice.

Update copies Company Name, Position, Link, and JD from that source tab onto the profile **main** tab (named after the profile title) and skips rows already present. Update All runs Update for every pipeline entry.

Forward archives that profile workbook: the current main tab is renamed to the next unused positive integer (`1`, then `2`, then `3`, … using max existing log number plus one). The listings stay on that numbered tab as a log. A new empty profile-formatted tab is created with the original name. Forward All runs Forward once per distinct profile in the table.

After Update or Forward, Flexis reapplies protection. On the named main tab the owner can edit every cell; invited editors can edit only Status and Issue. Numbered log tabs are owner-only.

## Consequences

Gmail must be connected. Pipeline entries live in `job_pipeline_entries`. Deleting an entry does not remove listings already written to the profile sheet. Update always writes to the named main tab, not a log tab.

## Related

- [008-job-catalog-google-sheets.md](008-job-catalog-google-sheets.md)
