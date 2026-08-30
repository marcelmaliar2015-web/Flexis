# Frontend UI

## Design system

MUI. Prefer native MUI props. Use `styled()` when the look cannot be achieved with props. Use `sx` only for layout on `Box`, `Stack`, and `Grid2`.

## Theme and layout

Theme: `frontend/src/app/providers/theme.ts`. Navy primary, copper secondary, warm paper background. Headlines use Fraunces; UI text uses Outfit (loaded in `frontend/index.html`). `CssBaseline` is applied in `AppProviders`.

Shell: `AppLayout` is a full-viewport column. Sticky paper `AppBar` with brand mark (links to `/`), Home, Health (signed in), Users (admin), Sign in or Sign out. Home is hidden below the `sm` breakpoint. Pages own their padding.

## Home landing

`HomePage` is the first screen at `/`. Hero band, live platform preview (shared health query), and three capability cards. Primary action is Sign in when anonymous, or View system health when signed in.

## Component patterns

Feature screens are functions in `features/*/`. Shared presentational pieces go in `shared/ui/` when a second feature needs them. That folder does not exist yet.

## Related

- [overview.md](overview.md)
- [structure.md](structure.md)
