# Frontend routing

## Router

React Router `createBrowserRouter` in `frontend/src/app/router/appRouter.tsx`. Layout route renders `AppLayout` and an `Outlet`. Authenticated product routes also render `AuthenticatedLayout`. Guards: `RequireAuth`, `RequireAdmin` in `routeGuards.tsx`.

## Routes

| Path | Screen | Access |
| --- | --- | --- |
| `/` | `HomePage` | Public |
| `/sign-in` | `SignInPage` | Public |
| `/dashboard` | `DashboardPage` | Authenticated |
| `/job-application` | `JobApplicationPage` | Authenticated |
| `/settings` | `SettingsPage` | Authenticated |
| `/health` | `HealthPage` | Authenticated |
| `/users` | `UsersPage` | Admin |

Sign-in and already-signed-in visits to `/sign-in` go to `/dashboard`. Non-admins hitting `/users` redirect to `/dashboard`. Unauthenticated visits to protected routes redirect to `/sign-in` with `state.from`.

## Guards and access

`RequireAuth` wraps `AuthenticatedLayout`. `RequireAdmin` wraps `/users` only.

## Related

- [overview.md](overview.md)
- [../backend/security.md](../backend/security.md)
- [../decisions/005-signed-in-left-nav.md](../decisions/005-signed-in-left-nav.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
