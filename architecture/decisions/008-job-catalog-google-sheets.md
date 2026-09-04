# Job catalog Google Sheets

## Context

Job Application profiles and sources need Google Sheets workbooks created through the connected account, not pasted URLs. Source workbooks need extra tabs for locations.

## Decision

Creating a profile or source calls the Google Sheets API with the stored refresh token. The catalog item stores `SpreadsheetId` and the spreadsheet URL. A profile workbook has a main tab named after the profile title and a `Profile` info tab (Field / Value) for optional personal details. Forward on Operations archives that main tab as a numbered log (`1`, `2`, `3`, …) and creates a new empty main tab with the original name; see [010-job-application-pipeline.md](010-job-application-pipeline.md). A source workbook starts with a `US` tab. Further source tabs are locations, edited on Job Application Settings. Operations is a Pipeline table that copies listings from a source location onto a profile main tab. Profile listing tabs (main and numbered archive tabs) use a Google Sheets table with a chip-shaped Status dropdown: Applied, Interview, Banned, Invalid, Expired, Other, each with its own color. `EnsureProfileStatusDropdownAsync` upgrades existing profile listing tabs on financial board load and banned-match scans. Source tabs do not include Status. The connected Google owner can edit every cell. Invited editors can edit only Status and Issue on the named profile main tab. Numbered profile log tabs, the `Profile` info tab, and source tabs are owner-only to edit; invited users can still view them when the spreadsheet is shared.

## Consequences

Gmail must be connected before create. Title-only writes; the API sets the sheet URL and the UI shows that URL after create. New sheets are moved into `Flexis` / `Job Application` / `Profiles` or `Sources`. Every tab uses a fixed 21 pixel row height. Body cells use black text and wrap with no overflow. The header row keeps navy background and light text. Appending listings expands the sheet grid (and listings table range) when the write would exceed current row or column limits, so Update does not fail with Google's grid-limit error. Deleting a catalog item deletes the Drive file Flexis created and its pipeline entries. Do not ask for broader Drive scope. See [011-google-drive-folder-layout.md](011-google-drive-folder-layout.md).
