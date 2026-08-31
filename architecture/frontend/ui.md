# Frontend UI

## Design system

MUI. Prefer native MUI props. Use `styled()` when the look cannot be achieved with props. Use `sx` only for layout on `Box`, `Stack`, and `Grid2`.

## Theme and layout

Theme: `frontend/src/app/providers/theme.ts`. Navy primary, copper secondary, warm paper background. Headlines use Fraunces; UI text uses Outfit (loaded in `frontend/index.html`). `CssBaseline` is applied in `AppProviders`. `MuiTableCell` default `align` is `center`.

Shell: `AppLayout` is a full-viewport column (`height: 100dvh`, overflow hidden). Sticky paper `AppBar` with brand mark (links to `/dashboard` when signed in, otherwise `/`) and either Gmail status plus `UserMenu` or Sign in. The main region fills the rest of the viewport. Pages own their padding.

`UserMenu` is an initials avatar. The menu header is avatar, display name, email, and a role chip, then Settings, Help, and Sign out.

`GoogleSyncStatus` sits left of `GoogleConnectStatus`. A three-lamp bar and `Updated x mins ago` show last successful Google sync. Green is under 2 minutes, amber under 8, red after that or on failure. Lamps chase while a sync runs. Click runs a full sheet and configuration refresh. Auto sync is every 3 minutes while the tab is visible. `GoogleConnectStatus` sits left of `UserMenu` when signed in. A live, waiting, or idle orb plus Gmail and (from `sm` up) the address or short state. The menu explains the connection and links to Job Application, Settings, or Help. Connect and disconnect stay on Job Application Settings. `NotificationCenter` sits left of Google sync when signed in, and left of Sign in for visitors. Issues opens a drawer of errors and warnings with time, source, request, status, and message. Copy all copies that log. A snackbar shows each new issue. The API appends the same facts to `.flexis/issue-log.jsonl`.

Authenticated product routes use `AuthenticatedLayout`: a left panel with Dashboard, Job Application, Mail Check, Settings, and Help, then the page `Outlet`. The left panel stays in place; only the content pane scrolls. The pane keeps a stable scrollbar gutter so headings and tabs stay aligned when content length changes. Dashboard is a workspace status board: platform health, Google Cloud client, Gmail, catalog counts, listing and price KPIs, listing status mix, price by pipeline row, setup attention, seven-day activity, a pipeline contribution table, and Admin user counts. Sheet counts stay zero until Gmail can read profile workbooks. Settings always has a Your account card (display name and optional password; email and role are read-only). Settings also shows the Google Cloud client form and other users when the signed-in role is Admin (create, edit, delete; last active admin cannot be deleted). The users table does not include the signed-in account. Help uses MUI Tabs: Overview, Google setup, Operations, Financial, Logs, Mail Check, and Problems. Overview is the product map: these Help tabs, left nav, header, Job Application tabs, roles, and first run. Opening a guide row switches Help tabs. After a tab is opened it stays mounted and is hidden when inactive. Google setup is the Gmail connect Google Cloud guide and shows the full redirect URI `http://localhost:5080/api/google/connections/callback` with copy. Job Application uses MUI Tabs: Operations, Financial, Logs, then Settings. After a tab is opened it stays mounted and is hidden when inactive. Operations is a Pipeline table (`profile`, `source` with location). A row click opens the pipeline entry page for profile and source edits, Update, Forward, Delete, banned companies, and live banned matches. Update All, Forward All, and Delete All stay on the table. Financial lists each pipeline row with profile, source, listing total, applied, interviews, apply rate, bonus rate, and price, plus all-rows and selected-rows totals. Logs is a searchable, category-filtered activity feed grouped by day. Settings has Gmail connect, default apply and bonus rates, profile and source tables (`title`, `createdAt`, spreadsheet URL), and source location tabs. Creating a profile or source creates a Google Sheet under `Flexis` / `Job Application` / `Profiles` or `Sources` and then shows that URL. Help describes that Drive tree. Catalog and Operations actions stay disabled until that user has connected Gmail. Connect Gmail opens Google. Copy URL copies the consent URL for another browser. Disconnect stays enabled when connected. Unconfigured Google Cloud client links admins to Settings and others to Help. Mail Check uses MUI Tabs: Inbox, Check, Settings. Inbox lists labeled and pinned mail with label chips. Check runs a pass now and shows last-run counts. Settings has mailbox connect (Gmail active; Outlook planned), plus OpenAI key and model. Auto-check runs about every two minutes while the browser tab is visible when a mailbox is connected and a key is saved.

## Home landing

`HomePage` is the visitor landing at `/`. Hero band, live platform preview (shared health query), and three capability cards. Primary action is Sign in. Signed-in visits are sent to Dashboard.

## Component patterns

Feature screens are functions in `features/*/`. Shared presentational pieces go in `shared/ui/` when a second feature needs them. That folder does not exist yet.

## Related

- [overview.md](overview.md)
- [structure.md](structure.md)
- [../decisions/005-signed-in-left-nav.md](../decisions/005-signed-in-left-nav.md)
- [../decisions/007-account-menu.md](../decisions/007-account-menu.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
- [../decisions/008-job-catalog-google-sheets.md](../decisions/008-job-catalog-google-sheets.md)
- [../decisions/009-google-client-in-settings.md](../decisions/009-google-client-in-settings.md)
- [../decisions/010-job-application-pipeline.md](../decisions/010-job-application-pipeline.md)
- [../decisions/011-google-drive-folder-layout.md](../decisions/011-google-drive-folder-layout.md)
- [../decisions/012-pipeline-banned-companies.md](../decisions/012-pipeline-banned-companies.md)
- [../decisions/013-job-application-financial-logs.md](../decisions/013-job-application-financial-logs.md)
- [../decisions/014-header-google-status.md](../decisions/014-header-google-status.md)
- [../decisions/015-google-workspace-sync.md](../decisions/015-google-workspace-sync.md)
- [../decisions/016-dashboard-workspace-status.md](../decisions/016-dashboard-workspace-status.md)
- [../decisions/017-signed-in-account-profile.md](../decisions/017-signed-in-account-profile.md)
- [../decisions/018-guest-only-public-routes.md](../decisions/018-guest-only-public-routes.md)
- [../decisions/019-mail-check.md](../decisions/019-mail-check.md)
- [../decisions/020-issue-notifications.md](../decisions/020-issue-notifications.md)
