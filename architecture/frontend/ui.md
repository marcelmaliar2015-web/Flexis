# Frontend UI

## Design system

MUI. Prefer native MUI props. Use `styled()` when the look cannot be achieved with props. Use `sx` only for layout on `Box`, `Stack`, and `Grid2`.

## Theme and layout

Theme: `frontend/src/app/providers/theme.ts`. Navy primary, copper secondary, warm paper background. Headlines use Fraunces; UI text uses Outfit (loaded in `frontend/index.html`). `CssBaseline` is applied in `AppProviders`.

Shell: `AppLayout` is a full-viewport column. Sticky paper `AppBar` with brand mark (links to `/dashboard` when signed in, otherwise `/`), Home, Health (signed in), Users (admin), Sign in or Sign out. Home is hidden below the `sm` breakpoint. Pages own their padding.

Authenticated product routes use `AuthenticatedLayout`: a left panel with Dashboard, Job Application, and Settings buttons, then the page `Outlet`. Dashboard and Settings are empty. Job Application has a Gmail connect card.

## Home landing

`HomePage` is the first screen at `/`. Hero band, live platform preview (shared health query), and three capability cards. Primary action is Sign in when anonymous, or Open dashboard when signed in.

## Component patterns

Feature screens are functions in `features/*/`. Shared presentational pieces go in `shared/ui/` when a second feature needs them. That folder does not exist yet.

## Related

- [overview.md](overview.md)
- [structure.md](structure.md)
- [../decisions/005-signed-in-left-nav.md](../decisions/005-signed-in-left-nav.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
