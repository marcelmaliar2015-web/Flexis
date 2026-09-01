# Profile banned companies

## Context

A profile can exclude employers that should not be copied from sources or left unnoticed on its main sheet. Names on sheets vary (Ltd, LLC, US, punctuation) but matching must not treat unrelated firms as the same.

## Decision

Banned companies belong to one profile (`job_profile_banned_companies`). The pipeline entry detail page edits the paired profile's info and banned list. Sheet matches refresh on the Google workspace sync interval (3 minutes) and on manual sync. The scan reads that profile's main tab only. Matching rows get Status `Banned` on the sheet. Update skips listings that match a profile ban (`banned` on the result).

`CompanyNameMatcher` builds many normalized match keys per company name (spaced and compact forms, legal and domain suffix stripping, generic descriptor and location stripping, acronyms, and domain tokens from URLs). Two names match when any key from one overlaps any key from the other. Keys must be at least two characters and not all digits. Names that produce no usable keys are rejected.

## Consequences

Gmail must be connected to scan. Duplicate bans that match an existing ban are `409`. Deleting a profile deletes its bans.

## Related

- [010-job-application-pipeline.md](010-job-application-pipeline.md)
