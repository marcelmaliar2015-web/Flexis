# Job Application listing search

## Context

Users needed one place to find listings across every profile main sheet and a shared pool of listings that are not tied to a single profile workbook.

## Decision

Job Application adds a Search tab. Flexis ensures a Drive workbook `search-base` under `Flexis` / `Job Application` with a `Main` tab. Columns match a profile main sheet plus a final `Profile` string column: Company Name, Position, Link, JD, Download, Status, Issue, Profile.

`GET /api/job-application/search` returns profile options, Status values, and the search-base URL. `POST /api/job-application/search` queries the Postgres listing projection (`profile_main`, `profile_archive`, and `search_base`), applies Profile and Status as exact filters when set, and ranks Company Name, Position, Link, and JD with weighted fuzzy scoring (exact and contains boosts, token coverage, normalized Levenshtein, company-name matcher, link normalization, JD phrase windows). An empty projection triggers SyncDirty before search. Results include rank, similarity percent, source (`profile`, `profile-archive` or `profile-archive:{tab}`, or `search-base`), and listing fields, ordered by score then profile and company. The Search tab offers **Reindex from Sheets** (`POST /api/job-application/listings/reindex`) after out-of-band sheet edits. See [036-job-listing-projection.md](036-job-listing-projection.md).

## Consequences

Search requires a connected Job Application Gmail account. Empty text filters browse all projected rows that pass Profile/Status filters at score 100. Sheets remain the human edit surface; Postgres is the search read model.

## Related

- [008-job-catalog-google-sheets.md](008-job-catalog-google-sheets.md)
- [011-google-drive-folder-layout.md](011-google-drive-folder-layout.md)
- [013-job-application-financial-logs.md](013-job-application-financial-logs.md)
- [036-job-listing-projection.md](036-job-listing-projection.md)
