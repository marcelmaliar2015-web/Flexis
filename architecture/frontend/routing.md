# Frontend routing

React Router `createBrowserRouter` in `frontend/src/app/router/appRouter.tsx`. Layout route renders `AppLayout` and an `Outlet`. Authenticated product routes also render `AuthenticatedLayout`. Guard: `RequireAuth` in `routeGuards.tsx`.

## Routes

| Path | Screen | Access |
| --- | --- | --- |
| `/` | `HomePage` | Public |
| `/sign-in` | `SignInPage` | Public |
| `/dashboard` | `DashboardPage` | Authenticated |
| `/job-application` | `JobApplicationPage` | Authenticated |
| `/job-application/pipeline/:entryId` | `JobApplicationPipelineEntryPage` | Authenticated |
| `/settings` | `SettingsPage` | Authenticated |
| `/help` | `HelpPage` | Authenticated |
| `/health` | `HealthPage` | Authenticated |
| `/users` | Redirect to `/settings` | Authenticated |

Sign-in and already-signed-in visits to `/sign-in` go to `/dashboard`. Unauthenticated visits to protected routes redirect to `/sign-in` with `state.from`. Admin Google Cloud client and user management are on `/settings`, not a separate screen. `/health` has no nav link.

## Guards and access

`RequireAuth` wraps `AuthenticatedLayout`. Settings shows the Google Cloud client and the users table only when `role` is `Admin`.

## Related

- [overview.md](overview.md)
- [../backend/security.md](../backend/security.md)
- [../decisions/005-signed-in-left-nav.md](../decisions/005-signed-in-left-nav.md)
- [../decisions/007-account-menu.md](../decisions/007-account-menu.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
- [../decisions/009-google-client-in-settings.md](../decisions/009-google-client-in-settings.md)
- [../decisions/010-job-application-pipeline.md](../decisions/010-job-application-pipeline.md)
- [../decisions/012-pipeline-banned-companies.md](../decisions/012-pipeline-banned-companies.md)
