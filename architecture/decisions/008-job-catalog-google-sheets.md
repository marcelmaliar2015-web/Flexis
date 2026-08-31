# Job catalog Google Sheets

## Context

Job Application profiles and sources need Google Sheets workbooks created through the connected account, not pasted URLs. Source workbooks need extra tabs for locations.

## Decision

Creating a profile or source calls the Google Sheets API with the stored refresh token. The catalog item stores `SpreadsheetId` and the spreadsheet URL. A profile workbook has one tab named after the profile title. A source workbook starts with a `US` tab. Further source tabs are locations, edited on Job Application Settings. Operations is a Pipeline table that copies listings from a source location onto a profile sheet. Profile Status is a dropdown: Applied, Invalid, Expired, Other, each with its own color. Source tabs do not include Status. The connected Google owner can edit every cell. Invited editors can edit only Status and Issue on profile tabs. Source tabs are owner-only.

## Consequences

Gmail must be connected before create. Title-only writes; the API sets the sheet URL and the UI shows that URL after create. New sheets are moved into `Flexis` / `Job Application` / `Profiles` or `Sources`. Every tab uses a fixed 21 pixel row height. Body cells use black text and wrap with no overflow. The header row keeps navy background and light text. Deleting a catalog item deletes the Drive file Flexis created and its pipeline entries. Do not ask for broader Drive scope. See [011-google-drive-folder-layout.md](011-google-drive-folder-layout.md).
