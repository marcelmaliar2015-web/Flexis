# Frontend UI

## Design system

MUI. Prefer native MUI props. Use `styled()` when the look cannot be achieved with props. Use `sx` only for layout on `Box`, `Stack`, and `Grid2`.

## Theme and layout

Theme: `frontend/src/app/providers/theme.ts`. Navy primary, copper secondary, warm paper background. Headlines use Fraunces; UI text uses Outfit (loaded in `frontend/index.html`). `CssBaseline` is applied in `AppProviders`. `MuiTableCell` default `align` is `center`.

Shell: `AppLayout` is a full-viewport column (`height: 100dvh`, overflow hidden). Sticky paper `AppBar` with brand mark (links to `/dashboard` when signed in, otherwise `/`) and either client integrations, Gmail status plus `UserMenu` or Sign in. The main region fills the rest of the viewport. Pages own their padding.

`UserMenu` is an initials avatar. The menu header is avatar, display name, email, and a role chip, then Settings, Help, and Sign out.

`ClientIntegrationsStatus` sits left of `GoogleSyncStatus` when signed in. Google and Microsoft orbs show whether the admin OAuth apps are ready (`configured` / `outlookAvailable`). The menu explains each client and links to Settings (admin) or Help. `MailCheckAutoStatus` sits left of client integrations when server auto-check is live for the signed-in user (enabled, OpenAI key saved, and at least one mailbox connected). A navy pill with a pulsing scan beacon shows the watch state; while classifying it becomes an indigo gradient pill with shimmer, white spinner, and `Classifying mail…`. A menu shows interval, last classified time, and Open Mail Check. `GoogleSyncStatus` sits left of `GoogleConnectStatus`. A three-lamp bar and `Updated x mins ago` show last successful Google sync. Green is under 2 minutes, amber under 8, red after that or on failure. While a sync runs the lamps swap for a small circular percent ring driven by weighted sync steps (pipeline, catalogs, per-profile banned scans, financial, logs on manual refresh). Click runs a full sheet and configuration refresh. Auto sync is every 3 minutes while the tab is visible. `GoogleConnectStatus` sits left of `UserMenu` when signed in. A live, waiting, or idle orb plus Gmail and (from `sm` up) the address or short state. The menu explains the connection and links to Job Application, Settings, or Help. Connect and disconnect stay on Job Application Settings. `NotificationCenter` sits left of client integrations when signed in, and left of Sign in for visitors. Issues opens a drawer of errors and warnings with time, source, request, status, and message. Copy all copies that log. A snackbar shows each new issue. The API appends the same facts to `.flexis/issue-log.jsonl`.

Authenticated product routes use `AuthenticatedLayout`: a left panel with Dashboard, Job Application, Mail Check, Settings, and Help, then the page `Outlet`. The left panel stays in place; only the content pane scrolls. The pane keeps a stable scrollbar gutter so headings and tabs stay aligned when content length changes. Dashboard is a workspace status board: platform health, Google Cloud client, Gmail, catalog counts, listing and price KPIs, listing status mix, price by pipeline row, setup attention, seven-day activity, a pipeline contribution table, and Admin user counts. Sheet counts stay zero until Gmail can read profile workbooks. Settings always has a Your account card (display name and optional password; email and role are read-only). Settings also shows the Google Cloud client form, Microsoft client form, and other users when the signed-in role is Admin (create, edit, delete; last active admin cannot be deleted). The users table does not include the signed-in account. Help uses MUI Tabs: Overview, Google setup, Microsoft setup, Operations, Financial, Logs, Mail Check, and Problems. Overview is the product map: these Help tabs, left nav, header, Job Application tabs, roles, and first run. Opening a guide row switches Help tabs. After a tab is opened it stays mounted and is hidden when inactive. Google setup is the Gmail connect Google Cloud guide and shows the full redirect URI `http://localhost:5080/api/google/connections/callback` with copy. Microsoft setup is the Mail Check Outlook Azure guide. Eight steps in order. Each step has a goal, numbered actions, and a done-when line. Step 1 opens a free Azure directory for app registration and states Microsoft Graph Outlook mail is not a paid or metered mail API. Redirect URI `http://localhost:5080/api/mail-check/mailbox/outlook/callback` with copy appears only in step 4. Job Application uses MUI Tabs: Operations, Financial, Logs, then Settings. After a tab is opened it stays mounted and is hidden when inactive. Operations is a Pipeline table (`profile`, `source` with location). A row click opens the pipeline entry page for profile and source edits, profile info, Update, Forward, Delete, that profile's banned companies, and live banned matches on the profile main sheet. Update All, Forward All, and Delete All stay on the table. A page-level `PipelineBulkProgress` banner above Job Application tabs shows row counts, the current profile or source, and a progress bar while bulk actions run. Financial lists each pipeline row with Today, Archived, and Lifetime columns. Today uses the profile main tab. Archived uses numbered sheets from Forward. Lifetime is both combined. A Performance chart shows hourly today, archived, and lifetime price from stored snapshots, with a Daily toggle that uses the last snapshot of each day. Open Financial after listings have a Status of Applied or Interview. Dashboard workspace KPIs still use today main-tab counts. New profile opens a dialog with title, profile info fields, and resume generation fields; filled values use the same save paths as Settings and Resume generation. Creating a profile or source creates a Google Sheet under `Flexis` / `Job Application` / `Profiles` or `Sources` and then shows that URL. Help describes that Drive tree. Catalog and Operations actions stay disabled until that user has connected Gmail. Connect Gmail opens Google. Copy URL copies the consent URL for another browser. Disconnect stays enabled when connected. Unconfigured Google Cloud client links admins to Settings and others to Help. Mail Check uses MUI Tabs: Need action, Inbox, Check, Log, Settings. Need action lists pin-configured need-action labels. Inbox lists labeled and pinned mail from every connected mailbox with a Mailbox column and label chips. Check has Check all across every connected mailbox and per-mailbox Check (inbox and junk for Outlook; inbox, spam, and categories for Gmail). The Check tab shows a progress panel with live stage, lock-wait status, elapsed time, session timing bars, and per-mailbox stats. Up to three messages per server round; Check all repeats until caught up. Each round also enforces trash actions on already-labeled inbox mail. Log is a sticky paginated table of durable auto and manual actions with source, mailbox, from, subject, label, action, duration, and detail; chip filters, mailbox select, and search hit the server. A page-level banner appears during manual check; refreshing mid-check shows a warning because the request aborts while auto-check may continue in the AppBar. Sent, drafts, and archive are not scanned. Background auto-check runs every 20 seconds while the Mail Check page is open and visible when enabled. Settings lists connected mailboxes with per-row Disconnect, Add Gmail, Add Outlook when the Microsoft client is configured, plus OpenAI key and model.

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
- [../decisions/028-financial-performance-snapshots.md](../decisions/028-financial-performance-snapshots.md)
- [../decisions/014-header-google-status.md](../decisions/014-header-google-status.md)
- [../decisions/022-header-client-integrations.md](../decisions/022-header-client-integrations.md)
- [../decisions/015-google-workspace-sync.md](../decisions/015-google-workspace-sync.md)
- [../decisions/016-dashboard-workspace-status.md](../decisions/016-dashboard-workspace-status.md)
- [../decisions/017-signed-in-account-profile.md](../decisions/017-signed-in-account-profile.md)
- [../decisions/018-guest-only-public-routes.md](../decisions/018-guest-only-public-routes.md)
- [../decisions/019-mail-check.md](../decisions/019-mail-check.md)
- [../decisions/023-multi-mailbox-mail-check.md](../decisions/023-multi-mailbox-mail-check.md)
- [../decisions/020-issue-notifications.md](../decisions/020-issue-notifications.md)
