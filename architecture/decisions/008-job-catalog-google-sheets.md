# Job catalog Google Sheets

## Context

Job Application profiles and sources need Google Sheets workbooks created through the connected account, not pasted URLs. Source workbooks need extra tabs for locations.

## Decision

Creating a profile or source calls the Google Sheets API with the stored refresh token. The catalog item stores `SpreadsheetId` and the spreadsheet URL. A profile workbook has one tab named after the profile title. A source workbook starts with a `US` tab. Further source tabs are locations, edited on the Operations tab. Status is a dropdown: Applied, Invalid, Expired, Other, each with its own color.

## Consequences

Gmail must be connected before create. Title-only writes; the API sets the sheet URL. Deleting a catalog item deletes the Drive file Flexis created. Do not ask for broader Drive scope.
