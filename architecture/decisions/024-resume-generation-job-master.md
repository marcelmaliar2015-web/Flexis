# Resume generation job-master sheet

## Context

Resume generation needs per-profile prompt, resume style (1–14), and owner. Operators also need a shared owner dropdown list they can extend. Downstream tooling reads a consolidated job-master workbook instead of querying Flexis directly.

## Decision

Job Application has a Resume generation tab. Per-user owner options and per-profile resume fields live in PostgreSQL (`job_resume_settings`, `job_profile_resume_settings`). Saving owner options or profile resume settings creates or updates a `job-master` Google Sheet in the Flexis Drive root folder (not under Job Application). That workbook has a `Profile Management` tab with columns Name, Tab, Sheet, Prompt, Resume Style, Owner. Name and Tab equal the profile title (main tab name). Sheet is the profile spreadsheet URL. Rows appear only when a profile has at least one of prompt, resume style, or owner set. The tab shows the job-master URL after the first sync.

## Consequences

Gmail must be connected to save resume settings or sync job-master. Profile rename and delete re-sync job-master. See [011-google-drive-folder-layout.md](011-google-drive-folder-layout.md) for the Flexis folder tree.
