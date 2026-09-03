# Backend API

## Style

ASP.NET Core controllers. JSON camelCase. Enums as strings. Route prefix `api/`. CORS policy `Frontend` allows `Frontend:Origins`. OpenAPI mapped in Development at `/openapi/v1.json` (anonymous). Application exceptions map to Problem Details (`401`, `400`, `404`, `409`, `500`).

## Endpoints

| Method | Path | Access | Response |
| --- | --- | --- | --- |
| GET | `/api/health` | Anonymous | `200` or `503` + `HealthStatusDto` |
| POST | `/api/auth/sign-in` | Anonymous | `200` + `SignInResultDto` |
| GET | `/api/auth/me` | Authenticated | `200` + `UserDto` |
| PUT | `/api/auth/me` | Authenticated | `200` + `UserDto` |
| GET | `/api/users` | Admin | `200` + `UserDto[]` |
| POST | `/api/users` | Admin | `201` + `UserDto` |
| PUT | `/api/users/{id}` | Admin | `200` + `UserDto` |
| DELETE | `/api/users/{id}` | Admin | `204` |
| GET | `/api/google/client` | Admin | `200` + `GoogleClientSettingsDto` |
| PUT | `/api/google/client` | Admin | `200` + `GoogleClientSettingsDto` |
| GET | `/api/microsoft/client` | Admin | `200` + `MicrosoftClientSettingsDto` |
| PUT | `/api/microsoft/client` | Admin | `200` + `MicrosoftClientSettingsDto` |
| GET | `/api/google/connections` | Authenticated | `200` + `GoogleConnectionStatusDto` |
| POST | `/api/google/connections/start` | Authenticated | `200` + `GoogleConnectStartDto` |
| GET | `/api/google/connections/callback` | Anonymous | `302` to the Job Application return URL |
| DELETE | `/api/google/connections` | Authenticated | `204` |
| GET | `/api/job-application/profiles` | Authenticated | `200` + `JobCatalogItemDto[]` from PostgreSQL |
| POST | `/api/job-application/profiles` | Authenticated | `201` + `JobCatalogItemDto` |
| PUT | `/api/job-application/profiles/{id}` | Authenticated | `200` + `JobCatalogItemDto` |
| DELETE | `/api/job-application/profiles/{id}` | Authenticated | `204` |
| GET | `/api/job-application/profiles/{id}/info` | Authenticated | `200` + `ProfileInfoDto` |
| PUT | `/api/job-application/profiles/{id}/info` | Authenticated | `200` + `ProfileInfoDto` |
| GET | `/api/job-application/profiles/{id}/banned-companies` | Authenticated | `200` + `ProfileBannedCompanyDto[]` |
| POST | `/api/job-application/profiles/{id}/banned-companies` | Authenticated | `201` + `ProfileBannedCompanyDto` |
| PUT | `/api/job-application/profiles/{id}/banned-companies/{companyId}` | Authenticated | `200` + `ProfileBannedCompanyDto` |
| DELETE | `/api/job-application/profiles/{id}/banned-companies/{companyId}` | Authenticated | `204` |
| GET | `/api/job-application/profiles/{id}/banned-matches` | Authenticated | `200` + `ProfileBannedMatchesDto` |
| GET | `/api/job-application/sources` | Authenticated | `200` + `JobCatalogItemDto[]` from PostgreSQL |
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
| DELETE | `/api/job-application/pipeline` | Authenticated | `204` |
| DELETE | `/api/job-application/pipeline/{id}` | Authenticated | `204` |
| POST | `/api/job-application/pipeline/update-all` | Authenticated | `200` + `JobPipelineUpdateResultDto` |
| POST | `/api/job-application/pipeline/forward-all` | Authenticated | `200` + `JobPipelineBatchForwardResultDto` |
| POST | `/api/job-application/pipeline/{id}/update` | Authenticated | `200` + `JobPipelineUpdateResultDto` |
| POST | `/api/job-application/pipeline/{id}/forward` | Authenticated | `200` + `JobPipelineForwardResultDto` |
| GET | `/api/job-application/financial` | Authenticated | `200` + `JobFinancialBoardDto` |
| PUT | `/api/job-application/financial/defaults` | Authenticated | `200` + `JobFinancialDefaultsDto` |
| PUT | `/api/job-application/financial/rows/{entryId}/rates` | Authenticated | `200` + `JobFinancialRowDto` |
| GET | `/api/job-application/resume` | Authenticated | `200` + `JobResumeBoardDto` |
| PUT | `/api/job-application/resume/owner-options` | Authenticated | `200` + `JobResumeBoardDto` |
| PUT | `/api/job-application/resume/profiles/{profileId}` | Authenticated | `200` + `JobResumeBoardDto` |
| GET | `/api/job-application/logs` | Authenticated | `200` + `JobApplicationLogDto[]` |
| GET | `/api/mail-check/settings` | Authenticated | `200` + `MailCheckSettingsDto` |
| PUT | `/api/mail-check/settings` | Authenticated | `200` + `MailCheckSettingsDto` |
| GET | `/api/mail-check/models` | Authenticated | `200` + `MailCheckModelsDto` |
| POST | `/api/mail-check/run` | Authenticated | `200` + `MailCheckRunDto` |
| GET | `/api/mail-check/inbox` | Authenticated | `200` + `MailCheckInboxDto` |
| GET | `/api/mail-check/need-action` | Authenticated | `200` + `MailCheckInboxDto` |
| GET | `/api/mail-check/logs` | Authenticated | `200` + `MailCheckActionLogPageDto` |
| GET | `/api/mail-check/mailbox` | Authenticated | `200` + `MailMailboxStatusDto` |
| POST | `/api/mail-check/mailbox/gmail/start` | Authenticated | `200` + `MailConnectStartDto` |
| POST | `/api/mail-check/mailbox/outlook/start` | Authenticated | `200` + `MailConnectStartDto` |
| GET | `/api/mail-check/mailbox/outlook/callback` | Anonymous | Redirect to Mail Check with `?mailbox=` |
| DELETE | `/api/mail-check/mailbox/{id}` | Authenticated | `204` |
| POST | `/api/diagnostics/events` | Authenticated | `204` |

`UserDto`: `id`, `email`, `displayName`, `role`, `isActive`, `createdAt`. Role values: `Admin`, `User`, `Viewer`. `PUT /api/auth/me` body: `displayName`, `password` (omit or blank to keep the current password). Email, role, and active cannot change on that path. Admin user PUT body: `displayName`, `role`, `isActive`, `password` (optional).

`GoogleClientSettingsDto`: `clientId`, `hasSecret`. PUT body: `clientId`, `clientSecret` (omit or blank to keep the stored secret). The secret is never returned.

`MicrosoftClientSettingsDto`: `clientId`, `hasSecret`. PUT body: `clientId`, `clientSecret` (omit or blank to keep the stored secret). The secret is never returned.

`GoogleConnectionStatusDto`: `configured`, `connected`, `googleEmail`, `connectedAt`, `capabilities`. `configured` is true when an admin has saved the Google Cloud client (or config fallback is set) and `Google:RedirectUri` is set. `GoogleConnectStartDto`: `authorizationUrl`. Start body: `returnUrl` (origin must be in `Frontend:Origins`, path `/job-application`).

`JobCatalogItemDto`: `id`, `title`, `createdAt`, `url`, `spreadsheetId`. Write body: `title`. Create makes a Google Sheet in `Flexis` / `Job Application` / `Profiles` or `Sources`; `url` is that spreadsheet. Duplicate title per kind for that user: `409`. Missing item: `404`. Gmail must be connected to create, rename a sheet, delete a sheet, manage locations, or edit profile info. Profile title cannot be `Profile` (reserved for the info tab). Profile main tab name is the title. Each profile workbook also has a `Profile` info tab (Field / Value) for optional Name, Address, Mail, Password, LinkedIn, Phone, Sex, Target Rate (Monthly), Race, and Veteran Status. That tab is fully locked for invited editors (view only). `ProfileInfoDto` / PUT body use those fields as optional strings. Source first tab is `US`. Tabs use a fixed 21 pixel row height (Profile info tab uses 28). Body cells use black text and wrap with no overflow. The header keeps navy background and light text. The connected owner can edit every cell. Invited editors can edit only Status and Issue on the named profile main tab. Numbered profile log tabs, the Profile info tab, and source tabs are owner-only to edit.

`SourceLocationDto`: `sheetId`, `name`. Write body: `name`. Locations are source spreadsheet tabs. Duplicate name: `409`. Last location cannot be deleted: `409`.

`JobPipelineBoardDto`: `entries`, `profiles`, `sources` (each source includes `locations`). `JobPipelineEntryDto`: `id`, `profileId`, `sourceId`, `locationSheetId`, `locationName`, `createdAt`. Write body: `profileId`, `sourceId`, `locationSheetId`. Duplicate profile and source location: `409`. Delete All removes every pipeline entry for that user. Listings already on profile sheets stay. Update copies Company Name, Position, Link, and JD from that source tab onto the named profile main tab immediately after the last filled listing row, after removing blank rows between filled listings, and skips rows already present (`added`, `skipped`) or banned (`banned`). Bans are per profile. Update All does that for every pipeline entry. After Update, invited editors can edit Status and Issue only on rows still present in the current source; other listing rows are fully locked. Forward renames the current main tab to the next unused number (`1`, `2`, `3`, …) and creates a new empty main tab with the original name (`archivedSheetName`, `mainSheetName`). Forward All does that once per distinct profile (`forwarded`). Then it reapplies owner lock; invited editors can still edit Status and Issue on the new named main tab. Numbered log tabs stay owner-only.

`ProfileBannedCompanyDto`: `id`, `companyName`, `createdAt`. Write body: `companyName`. Duplicate after name folding: `409`. Too-generic name: `400`. `ProfileBannedMatchesDto`: `matches` array of `companyName`, `position`, `link`, `matchedBan`. Matches are scanned from that profile main tab only. Each matching row gets Status `Banned` on the sheet. See [012-pipeline-banned-companies.md](../decisions/012-pipeline-banned-companies.md).

`JobFinancialBoardDto`: `defaults` (`applyRate`, `bonusRate`), `rows`, `allPrice`, `allTotal`, `allApplied`, `allInterviews` (today main tab totals), `archivedAllPrice`, `archivedAllTotal`, `archivedAllApplied`, `archivedAllInterviews` (numbered archive tabs), `lifetimeAllPrice`, `lifetimeAllTotal`, `lifetimeAllApplied`, `lifetimeAllInterviews`. `JobFinancialRowDto`: `entryId`, `profileTitle`, `sourceLabel`, today fields (`total`, `applied`, `interviews`, `price`), archived fields (`archivedTotal`, `archivedApplied`, `archivedInterviews`, `archivedPrice`), lifetime fields (`lifetimeTotal`, `lifetimeApplied`, `lifetimeInterviews`, `lifetimePrice`), `applyRate`, `bonusRate`. Today counts come from the profile named main tab. Archived counts sum numbered tabs (`1`, `2`, `3`, …) from Forward. Price is `applied * applyRate + interviews * bonusRate` per period. Defaults PUT and row rates PUT body: `applyRate`, `bonusRate` (0 to 10000). Missing pipeline row: `404`. See [013-job-application-financial-logs.md](../decisions/013-job-application-financial-logs.md).

`JobResumeBoardDto`: `jobMasterUrl`, `ownerOptions`, `profiles` (`profileId`, `title`, `url`, `prompt`, `resumeStyle`, `owner`). Owner options PUT body: `ownerOptions` (string array). Profile resume PUT body: optional `prompt`, `resumeStyle` (1 to 14), `owner` (must be in saved owner options when set). Saving syncs the `job-master` spreadsheet in the Flexis Drive root with a `Profile Management` tab. Rows include profiles with any of prompt, resume style, or owner set. Gmail must be connected to save. See [024-resume-generation-job-master.md](../decisions/024-resume-generation-job-master.md).

`JobApplicationLogDto`: `id`, `occurredAt`, `category`, `action`, `summary`, `detail`. Newest 200 for that user. Categories: `pipeline`, `catalog`, `financial`, `resume`, `account`.

`MailCheckSettingsDto`: `hasApiKey`, `model`, `classifierPrompt`, `defaultClassifierPrompt`, `labelActions`, `defaultLabelActions`, `needActionLabels`, `defaultNeedActionLabels`, `autoCheckEnabled`, `autoCheckIntervalSeconds` (20), last-run counts, `mailboxes` (`id`, `provider`, `email`, `connectedAt`, `checkedNewestAt`, `checkedUntilAt`, `lastScanAt`, `scanCaughtUp`), `outlookAvailable`. PUT body: `apiKey` (omit or blank to keep), `clearApiKey`, `model`, optional `classifierPrompt`, optional `labelActions` (all nine label slugs required when sent), optional `needActionLabels` (at least one slug when sent), optional `autoCheckEnabled`. The key is never returned. `MailMailboxStatusDto`: `outlookAvailable`, `mailboxes` (same item shape). `POST /api/mail-check/mailbox/gmail/start` body: `returnUrl` (must be `/mail-check` on an allowed origin). Returns `authorizationUrl`. OAuth callback redirects with `?mailbox=connected|denied|error`. `DELETE /api/mail-check/mailbox/{id}` disconnects that Mail Check mailbox. `MailCheckModelsDto`: `models` (`id`, `recommended`) from OpenAI with the stored key. `MailCheckRunDto`: `busy`, `processed`, `labeled`, `trashed`, `skipped`, `errors`, `hasMore`, `scanned`, `alreadySeen`, `mailboxId`, `mailboxEmail`, `mailboxProvider`, `items` (each item includes `messageId`, classified `label`, mailbox action `action`, mailbox id/email/provider). Each run classifies one new message per mailbox. Run body: `force`, optional `mailboxId` (one mailbox; omit for all), `resetCursor` (restart candidate scan). `MailCheckInboxDto`: pinned messages from every connected mailbox; each item includes mailbox id/email/provider. Query `label` is `rejected`, `applied`, `schedule`, `scheduled`, `assessment`, `availability`, `success`, or `other` (pin-configured labels only). `GET /api/mail-check/need-action` returns pinned mail whose label is in `needActionLabels` and configured as pin. `GET /api/mail-check/logs` query: `page`, `pageSize` (default 50, max 100), optional `source` (`auto`|`manual`), optional `action`, optional `mailboxId`, optional `q`. Returns `MailCheckActionLogPageDto`: `items`, `page`, `pageSize`, `totalCount`, `totalPages`. Each `MailCheckActionLogDto`: `id`, `runId`, `occurredAt`, `source`, `mailboxId`, `mailboxEmail`, `mailboxProvider`, `messageId`, `subject`, `from`, `action`, `label`, `detail`, `durationMs`. Actions include `pin`, `trash`, `keep`, `already_checked`, `error`, and `run_completed`. No mailbox connected or missing OpenAI key: `400`. See [019-mail-check.md](../decisions/019-mail-check.md), [023-multi-mailbox-mail-check.md](../decisions/023-multi-mailbox-mail-check.md), and [026-mail-check-action-log.md](../decisions/026-mail-check-action-log.md).

`DiagnosticsEventRequest`: `severity` (`error` or `warning`), `source`, `message`, optional `method`, `path`, `status`, `detail`. Appends one JSON line to `.flexis/issue-log.jsonl`. See [020-issue-notifications.md](../decisions/020-issue-notifications.md).

## Contracts and errors

Unknown routes: framework 404. Invalid sign-in: `401`. Duplicate email: `409`. Last active admin cannot be demoted, deactivated, or deleted: `409`. Google start with a missing client or a bad return URL: `400`. Invalid catalog title or location: `400`. Sheets calls without a connected Gmail: `400`. Google Sheets errors: `400`. Mail Check without a connected mailbox or an OpenAI key: `400`. OpenAI errors: `400`. Health does not throw to the client; failed checks become unhealthy entries. API exceptions and client diagnostic events are appended to `.flexis/issue-log.jsonl`. Development 500 Problem Details use the exception message.

## Related

- [overview.md](overview.md)
- [security.md](security.md)
- [../frontend/state.md](../frontend/state.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
- [../decisions/008-job-catalog-google-sheets.md](../decisions/008-job-catalog-google-sheets.md)
- [../decisions/009-google-client-in-settings.md](../decisions/009-google-client-in-settings.md)
- [../decisions/010-job-application-pipeline.md](../decisions/010-job-application-pipeline.md)
- [../decisions/012-pipeline-banned-companies.md](../decisions/012-pipeline-banned-companies.md)
- [../decisions/013-job-application-financial-logs.md](../decisions/013-job-application-financial-logs.md)
- [../decisions/017-signed-in-account-profile.md](../decisions/017-signed-in-account-profile.md)
- [../decisions/019-mail-check.md](../decisions/019-mail-check.md)
- [../decisions/020-issue-notifications.md](../decisions/020-issue-notifications.md)
- [../decisions/026-mail-check-action-log.md](../decisions/026-mail-check-action-log.md)
