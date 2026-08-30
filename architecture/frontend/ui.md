# Frontend UI

## Design system

MUI. Prefer native MUI props. Use `styled()` when the look cannot be achieved with props. Use `sx` only for layout on `Box`, `Stack`, and `Grid2`.

## Theme and layout

Theme: `frontend/src/app/providers/theme.ts`. Navy primary, copper secondary, warm paper background. Headlines use Fraunces; UI text uses Outfit (loaded in `frontend/index.html`). `CssBaseline` is applied in `AppProviders`.

Shell: `AppLayout` is a full-viewport column. Sticky paper `AppBar` with brand mark (links to `/dashboard` when signed in, otherwise `/`) and either `UserMenu` or Sign in. Pages own their padding.

`UserMenu` is an initials avatar. The menu shows display name, email, role, then Settings, Help, and Sign out.

Authenticated product routes use `AuthenticatedLayout`: a left panel with Dashboard, Job Application, Settings, and Help, then the page `Outlet`. Dashboard is empty. Settings shows the Google Cloud client form and user management when the signed-in role is Admin (create, edit, delete; last active admin cannot be deleted). Help is the in-app Gmail connect Google Cloud guide. It shows the full redirect URI `http://localhost:5080/api/google/connections/callback` and can copy it. Job Application uses MUI Tabs: Operations then Settings. Operations is a Pipeline table (`profile`, `source` with location, Update). Settings has Gmail connect, profile and source tables (`title`, `createdAt`, spreadsheet URL), and source location tabs. Creating a profile or source creates a Google Sheet under `Flexis` / `Job Application` / `Profiles` or `Sources` and then shows that URL. Help describes that Drive tree. Catalog and Operations actions stay disabled until that user has connected Gmail. Connect Gmail opens Google. Copy URL copies the consent URL for another browser. Disconnect stays enabled when connected. Unconfigured Google Cloud client links admins to Settings and others to Help.

## Home landing

`HomePage` is the first screen at `/`. Hero band, live platform preview (shared health query), and three capability cards. Primary action is Sign in when anonymous, or Open dashboard when signed in.

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
