# Frontend routing

React Router `createBrowserRouter` in `frontend/src/app/router/appRouter.tsx`. Layout route renders `AppLayout` and an `Outlet`. Authenticated product routes also render `AuthenticatedLayout`. Guards: `RequireAuth` and `RequireGuest` in `routeGuards.tsx`.

## Routes

| Path | Screen | Access |
| --- | --- | --- |
| `/` | `HomePage` | Guest |
| `/sign-in` | `SignInPage` | Guest |
| `/dashboard` | `DashboardPage` | Authenticated |
| `/job-application` | `JobApplicationPage` | Authenticated |
| `/job-application/pipeline/:entryId` | `JobApplicationPipelineEntryPage` | Authenticated |
| `/mail-check` | `MailCheckPage` | Authenticated |
| `/logs` | `LogsPage` | Authenticated (tabs: Job Application, Mail Check) |
| `/settings` | `SettingsPage` | Authenticated (tabs: Account, Job Application, Mail Check; Admin tab for Admin) |
| `/help` | `HelpPage` | Authenticated |
| `/health` | `HealthPage` | Authenticated |
| `/users` | Redirect to `/settings` | Authenticated |

Signed-in visits to `/` or `/sign-in` go to `/dashboard`, or to `state.from` when sign-in started from a protected route. Unauthenticated visits to protected routes redirect to `/sign-in` with `state.from`. Settings uses tabs for Account, Job Application, Mail Check, and Admin. Admin Google Cloud client, Microsoft client, and other users are on the Admin tab. Logs uses tabs for Job Application and Mail Check activity. `/health` has no nav link.

## Guards and access

`RequireGuest` wraps `/` and `/sign-in`. `RequireAuth` wraps `AuthenticatedLayout`. Both wait for session restore before redirecting. Settings shows the Admin tab only when `role` is `Admin`. Every signed-in role can edit display name and password on the Account tab.

## Related

- [overview.md](overview.md)
- [../backend/security.md](../backend/security.md)
- [../decisions/005-signed-in-left-nav.md](../decisions/005-signed-in-left-nav.md)
- [../decisions/007-account-menu.md](../decisions/007-account-menu.md)
- [../decisions/006-google-oauth-job-application.md](../decisions/006-google-oauth-job-application.md)
- [../decisions/009-google-client-in-settings.md](../decisions/009-google-client-in-settings.md)
- [../decisions/010-job-application-pipeline.md](../decisions/010-job-application-pipeline.md)
- [../decisions/012-pipeline-banned-companies.md](../decisions/012-pipeline-banned-companies.md)
- [../decisions/016-dashboard-workspace-status.md](../decisions/016-dashboard-workspace-status.md)
- [../decisions/017-signed-in-account-profile.md](../decisions/017-signed-in-account-profile.md)
- [../decisions/018-guest-only-public-routes.md](../decisions/018-guest-only-public-routes.md)
- [../decisions/030-consolidated-settings-tabs.md](../decisions/030-consolidated-settings-tabs.md)
- [../decisions/031-consolidated-logs-page.md](../decisions/031-consolidated-logs-page.md)
