# Job application pipeline

## Context

Operations needs one table to send listings from a source location tab into a profile sheet. Repeating that must not duplicate rows already on the profile.

## Decision

Operations is a Pipeline table. Each entry pairs a profile with a source location (`Source title · location`). Update copies Company Name, Position, Link, and JD from that source tab onto the profile sheet. A listing is skipped when company, position, and link already match a profile row. The same profile and source location cannot be added twice. Source location tabs are managed on Job Application Settings.

## Consequences

Gmail must be connected. Pipeline entries live in `job_pipeline_entries`. Deleting an entry does not remove listings already written to the profile sheet.

## Related

- [008-job-catalog-google-sheets.md](008-job-catalog-google-sheets.md)
