# Pipeline banned companies

## Context

A pipeline row can exclude employers that should not be copied or left unnoticed on its profile and source sheets. Names on sheets vary (Ltd, LLC, US, punctuation) but matching must not treat unrelated firms as the same.

## Decision

Banned companies belong to one pipeline entry (`job_pipeline_banned_companies`). The detail page lists them and polls sheet matches every 10 seconds while open. The scan reads that row's source location tab and the profile main tab. Update skips listings that match a ban (`banned` on the result).

Matching folds case, accents, punctuation, and `&`/`+` to `and`, drops `the`/`and`, strips trailing legal and geo tokens (`ltd`, `llc`, `inc`, `us`, `usa`, `uk`, …) and trailing tlds, then merges single-letter initials (`AT&T`). Two names match when the remaining tokens are the same multiset, or when both compacted strings are at least 4 characters and equal (`Wal Mart` / `Walmart`). Extra distinctive words do not match (`Amazon` does not match `Amazon Web Services`). Names that strip to nothing are rejected.

## Consequences

Gmail must be connected to scan. Duplicate bans that normalize to the same key are `409`. Deleting a pipeline entry deletes its bans.

## Related

- [010-job-application-pipeline.md](010-job-application-pipeline.md)
