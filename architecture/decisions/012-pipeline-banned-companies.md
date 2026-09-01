# Profile banned companies

## Context

A profile can exclude employers that should not be copied from sources or left unnoticed on its main sheet. Names on sheets vary (Ltd, LLC, US, punctuation) but matching must not treat unrelated firms as the same.

## Decision

Banned companies belong to one profile (`job_profile_banned_companies`). The pipeline entry detail page edits the paired profile's info and banned list. Sheet matches refresh on the Google workspace sync interval (3 minutes) and on manual sync. The scan reads that profile's main tab only. Update skips listings that match a profile ban (`banned` on the result).

Matching folds case, accents, punctuation, and `&`/`+` to `and`, drops `the`/`and`, strips trailing legal and geo tokens (`ltd`, `llc`, `inc`, `us`, `usa`, `uk`, …) and trailing tlds, then merges single-letter initials (`AT&T`). Two names match when the remaining tokens are the same multiset, or when both compacted strings are at least 4 characters and equal (`Wal Mart` / `Walmart`). Extra distinctive words do not match (`Amazon` does not match `Amazon Web Services`). Names that strip to nothing are rejected.

## Consequences

Gmail must be connected to scan. Duplicate bans that normalize to the same key are `409`. Deleting a profile deletes its bans.

## Related

- [010-job-application-pipeline.md](010-job-application-pipeline.md)
