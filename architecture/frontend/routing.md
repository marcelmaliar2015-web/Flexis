# Frontend routing

## Router

React Router `createBrowserRouter` in `frontend/src/app/router/appRouter.tsx`. Layout route renders `AppLayout` and an `Outlet`. Guards: `RequireAuth`, `RequireAdmin` in `routeGuards.tsx`.

## Routes

| Path | Screen | Access |
| --- | --- | --- |
| `/` | `HomePage` | Public |
| `/sign-in` | `SignInPage` | Public |
| `/health` | `HealthPage` | Authenticated |
| `/users` | `UsersPage` | Admin |

## Guards and access

Unauthenticated visits to `/health` or `/users` redirect to `/sign-in` with `state.from`. Non-admins hitting `/users` redirect to `/health`.

## Related

- [overview.md](overview.md)
- [../backend/security.md](../backend/security.md)
