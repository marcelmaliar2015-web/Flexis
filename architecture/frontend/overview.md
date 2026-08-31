# Frontend overview

## Role

Browser UI for Flexis. Calls the HTTP API. Does not talk to databases.

## Tech stack

React 19, TypeScript, Vite, MUI, TanStack Query, React Router.

## Entry

`frontend/src/main.tsx` mounts `App`. `App` wraps providers and `RouterProvider`. Local dev: double-click `frontend/run.bat` or run `npm run dev` in `frontend/` (`http://127.0.0.1:5173`).

## Boundaries

- `app/` owns shell, theme, router, providers, route guards, and the signed-in left nav.
- `features/` owns screens and feature hooks.
- `shared/` owns API client, session, config, and shared types.
- Features do not import other features. `shared/` does not import `app/` or `features/`.

## Related

- [structure.md](structure.md)
- [routing.md](routing.md)
- [state.md](state.md)
- [ui.md](ui.md)
- [../backend/overview.md](../backend/overview.md)
