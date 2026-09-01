# Google Drive folder layout

## Context

Job Application creates Google Sheets on the connected account. Those files must live in a stable, named tree in that user's Drive, not loose in My Drive. Flexis only has `drive.file`, so it can create and move files it owns, not browse the rest of Drive.

## Decision

Opening Job Application ensures the folder tree exists in that user's Drive. Creating a profile or source, connecting Gmail, or a layout change also moves catalog spreadsheets into the matching folder:

- `Flexis` — workspace root; also holds the `job-master` spreadsheet for resume generation (see [024-resume-generation-job-master.md](024-resume-generation-job-master.md))
- `Job Application` — Job Application files
- `Profiles` — one Sheet per profile
- `Sources` — one Sheet per source; location tabs stay in the workbook

New workbooks are created inside `Profiles` or `Sources`. Folder names and descriptions live in `Flexis.Application.Google.FlexisDriveLayout`. Folder IDs are stored on `google_connections`. If the Google account subject changes, stored IDs are cleared and the tree is resolved again. When IDs are missing, Flexis looks up app-created folders by name under the parent before creating a new folder. Existing catalog spreadsheets Flexis created are moved into the matching folder and removed from My Drive root. Disconnect does not delete the Drive tree or the sheets.

## Consequences

Do not request the full `drive` scope. Deleted Drive folders are recreated on the next ensure. See [006-google-oauth-job-application.md](006-google-oauth-job-application.md) and [008-job-catalog-google-sheets.md](008-job-catalog-google-sheets.md).
