# Frontend UI

## Design system

MUI. Prefer native MUI props. Use `styled()` when the look cannot be achieved with props. Use `sx` only for layout on `Box`, `Stack`, and `Grid2`.

## Theme and layout

Theme: `frontend/src/app/providers/theme.ts`. `CssBaseline` is applied in `AppProviders`. Shell: `AppLayout` with MUI `AppBar` and main `Outlet`.

## Component patterns

Feature screens are functions in `features/*/`. Shared presentational pieces go in `shared/ui/` when a second feature needs them. That folder does not exist yet.

## Related

- [overview.md](overview.md)
- [structure.md](structure.md)
