# Backend API

## Style

ASP.NET Core controllers. JSON camelCase. Enums as strings. Route prefix `api/`. CORS policy `Frontend` allows `Frontend:Origins`. OpenAPI mapped in Development at `/openapi/v1.json` (anonymous). Application exceptions map to Problem Details (`401`, `400`, `404`, `409`, `500`).

## Endpoints

| Method | Path | Access | Response |
| --- | --- | --- | --- |
| GET | `/api/health` | Anonymous | `200` or `503` + `HealthStatusDto` |
| POST | `/api/auth/sign-in` | Anonymous | `200` + `SignInResultDto` |
| GET | `/api/auth/me` | Authenticated | `200` + `UserDto` |
| GET | `/api/users` | Admin | `200` + `UserDto[]` |
| POST | `/api/users` | Admin | `201` + `UserDto` |
| PUT | `/api/users/{id}` | Admin | `200` + `UserDto` |
| DELETE | `/api/users/{id}` | Admin | `204` |
| GET | `/api/google/client` | Admin | `200` + `GoogleClientSettingsDto` |
| PUT | `/api/google/client` | Admin | `200` + `GoogleClientSettingsDto` |
| GET | `/api/google/connections` | Authenticated | `200` + `GoogleConnectionStatusDto` |
| POST | `/api/google/connections/start` | Authenticated | `200` + `GoogleConnectStartDto` |
| GET | `/api/google/connections/callback` | Anonymous | `302` to the Job Application return URL |
| DELETE | `/api/google/connections` | Authenticated | `204` |
| GET | `/api/job-application/profiles` | Authenticated | `200` + `JobCatalogItemDto[]` |
| POST | `/api/job-application/profiles` | Authenticated | `201` + `JobCatalogItemDto` |
| PUT | `/api/job-application/profiles/{id}` | Authenticated | `200` + `JobCatalogItemDto` |
| DELETE | `/api/job-application/profiles/{id}` | Authenticated | `204` |
| GET | `/api/job-application/sources` | Authenticated | `200` + `JobCatalogItemDto[]` |
| POST | `/api/job-application/sources` | Authenticated | `201` + `JobCatalogItemDto` |
| PUT | `/api/job-application/sources/{id}` | Authenticated | `200` + `JobCatalogItemDto` |
| DELETE | `/api/job-application/sources/{id}` | Authenticated | `204` |
| GET | `/api/job-application/sources/{id}/locations` | Authenticated | `200` + `SourceLocationDto[]` |
| POST | `/api/job-application/sources/{id}/locations` | Authenticated | `201` + `SourceLocationDto` |
| PUT | `/api/job-application/sources/{id}/locations/{sheetId}` | Authenticated | `200` + `SourceLocationDto` |
| DELETE | `/api/job-application/sources/{id}/locations/{sheetId}` | Authenticated | `204` |
| GET | `/api/job-application/pipeline` | Authenticated | `200` + `JobPipelineBoardDto` |
| POST | `/api/job-application/pipeline` | Authenticated | `201` + `JobPipelineEntryDto` |
| PUT | `/api/job-application/pipeline/{id}` | Authenticated | `200` + `JobPipelineEntryDto` |
| DELETE | `/api/job-application/pipeline/{id}` | Authenticated | `204` |
| POST | `/api/job-application/pipeline/update-all` | Authenticated | `200` + `JobPipelineUpdateResultDto` |
| POST | `/api/job-application/pipeline/forward-all` | Authenticated | `200` + `JobPipelineBatchForwardResultDto` |
| POST | `/api/job-application/pipeline/{id}/update` | Authenticated | `200` + `JobPipelineUpdateResultDto` |
| POST | `/api/job-application/pipeline/{id}/forward` | Authenticated | `200` + `JobPipelineForwardResultDto` |
| GET | `/api/job-application/pipeline/{id}/banned-companies` | Authenticated | `200` + `JobPipelineBannedCompanyDto[]` |
| POST | `/api/job-application/pipeline/{id}/banned-companies` | Authenticated | `201` + `JobPipelineBannedCompanyDto` |
| PUT | `/api/job-application/pipeline/{id}/banned-companies/{companyId}` | Authenticated | `200` + `JobPipelineBannedCompanyDto` |
| DELETE | `/api/job-application/pipeline/{id}/banned-companies/{companyId}` | Authenticated | `204` |
| GET | `/api/job-application/pipeline/{id}/banned-matches` | Authenticated | `200` + `JobPipelineBannedMatchesDto` |

`UserDto`: `id`, `email`, `displayName`, `role`, `isActive`, `createdAt`. Role values: `Admin`, `User`, `Viewer`.

`GoogleClientSettingsDto`: `clientId`, `hasSecret`. PUT body: `clientId`, `clientSecret` (omit or blank to keep the stored secret). The secret is never returned.

`GoogleConnectionStatusDto`: `configured`, `connected`, `googleEmail`, `connectedAt`, `capabilities`. `configured` is true when an admin has saved the Google Cloud client (or config fallback is set) and `Google:RedirectUri` is set. `GoogleConnectStartDto`: `authorizationUrl`. Start body: `returnUrl` (origin must be in `Frontend:Origins`, path `/job-application`).

`JobCatalogItemDto`: `id`, `title`, `createdAt`, `url`, `spreadsheetId`. Write body: `title`. Create makes a Google Sheet in `Flexis` / `Job Application` / `Profiles` or `Sources`; `url` is that spreadsheet. Duplicate title per kind for that user: `409`. Missing item: `404`. Gmail must be connected to create, rename a sheet, delete a sheet, or manage locations. Profile main tab name is the title. Source first tab is `US`. Tabs use a fixed 21 pixel row height, black body text, and wrap with no overflow. The header keeps navy background and light text. The connected owner can edit every cell. Invited editors can edit only Status and Issue on the named profile main tab. Numbered profile log tabs and source tabs are owner-only.

`SourceLocationDto`: `sheetId`, `name`. Write body: `name`. Locations are source spreadsheet tabs. Duplicate name: `409`. Last location cannot be deleted: `409`.

`JobPipelineBoardDto`: `entries`, `profiles`, `sources` (each source includes `locations`). `JobPipelineEntryDto`: `id`, `profileId`, `sourceId`, `locationSheetId`, `locationName`, `createdAt`. Write body: `profileId`, `sourceId`, `locationSheetId`. Duplicate profile and source location: `409`. Update copies Company Name, Position, Link, and JD from that source tab onto the named profile main tab and skips rows already present (`added`, `skipped`) or banned (`banned`). Update All does that for every pipeline entry. Forward renames the current main tab to the next unused number (`1`, `2`, `3`, …) and creates a new empty main tab with the original name (`archivedSheetName`, `mainSheetName`). Forward All does that once per distinct profile (`forwarded`). Then it reapplies owner lock; invited editors can still edit Status and Issue only on the named main tab. Numbered log tabs stay owner-only.

`JobPipelineBannedCompanyDto`: `id`, `companyName`, `createdAt`. Write body: `companyName`. Duplicate after name folding: `409`. Too-generic name: `400`. `JobPipelineBannedMatchesDto`: `source` and `profile` arrays of `sheet`, `companyName`, `position`, `link`, `matchedBan`. Matches are scanned from that entry's source location tab and profile main tab. See [012-pipeline-banned-companies.md](../decisions/012-pipeline-banned-companies.md).

## Contracts and errors

Unknown routes: framework 404. Invalid sign-in: `401`. Duplicate email: `409`. Last active admin cannot be demoted, deactivated, or deleted: `409`. Google start with a missing client or a bad return URL: `400`. Invalid catalog title or location: `400`. Sheets calls without a connected Gmail: `400`. Google Sheets errors: `400`. Health does not throw to the client; failed checks become unhealthy entries.

## Related

- [overview.md](overview.md)
- [security.md](security.md)
- [../frontend/state.md](../frontend/state.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
- [../decisions/008-job-catalog-google-sheets.md](../decisions/008-job-catalog-google-sheets.md)
- [../decisions/009-google-client-in-settings.md](../decisions/009-google-client-in-settings.md)
- [../decisions/010-job-application-pipeline.md](../decisions/010-job-application-pipeline.md)
- [../decisions/012-pipeline-banned-companies.md](../decisions/012-pipeline-banned-companies.md)
