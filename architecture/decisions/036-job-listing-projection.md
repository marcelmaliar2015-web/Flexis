# Job listing projection

## Context

Financial, Statistics, Search, banned-match scans, and AppBar Sheet refresh all read listing rows from Google Sheets. Free-tier Sheets quotas (~60 reads/min) are exhausted by profiles × archive tabs × refresh. Sheets remain the human edit surface; Postgres already holds catalogs, pipeline, rates, and status events but not the listing rows themselves.

## Decision

Keep Google Sheets as the write/edit workbook. Introduce a Postgres read model for listing rows:

- `job_listing_projections` — per-user rows with `Source` `profile_main` | `profile_archive` | `search_base`, optional `ProfileId` / `ArchiveTab`, listing fields, `ListingKey`, `RowNumber`, `ContentHash`, `SyncedAt`
- `job_sheet_sync_states` — per spreadsheet `DriveModifiedTime` and `LastSyncedAt`

`JobListingProjectionService` upserts from Update / Forward / ban Status writes using already-read in-memory rows when possible. `POST /api/job-application/listings/sync` pulls only spreadsheets whose Drive `modifiedTime` changed. `POST /api/job-application/listings/reindex` force-pulls every profile workbook and search-base.

Financial, Statistics, Search, and banned-match reads use the projection (empty projection triggers SyncDirty). Board cache TTL is 10 minutes. AppBar auto and manual Sheet refresh call SyncDirty then load boards from Postgres; manual no longer fans out banned-match scans. Search tab exposes Reindex from Sheets for out-of-band edits.

## Consequences

Idle sessions cost Drive metadata checks on the auto tick, not `values.get` per archive tab. Opening Dashboard / Financial / Search does not hit Sheets in the steady state. Update still writes Sheets, then projects without re-reading all archives. Out-of-band sheet edits need Reindex or a Drive-modified SyncDirty pull.

## Related

- [008-job-catalog-google-sheets.md](008-job-catalog-google-sheets.md)
- [012-pipeline-banned-companies.md](012-pipeline-banned-companies.md)
- [013-job-application-financial-logs.md](013-job-application-financial-logs.md)
- [015-google-workspace-sync.md](015-google-workspace-sync.md)
- [035-job-application-listing-search.md](035-job-application-listing-search.md)
