# Header Google connect status

## Context

Gmail connection is required for Job Application catalog and Operations. That status lived only on Job Application Settings, so it was easy to miss from Dashboard, Help, or product Settings. ADR 007 kept the AppBar to brand plus account.

## Decision

When signed in, the AppBar shows a Gmail status control on the right, to the left of `UserMenu`, separated by a hairline. It reads `googleConnectionQueryKey`. The pill shows a live, waiting, or idle orb, the word Gmail, and on `sm` and up the connected address or a short state. A menu explains the state and sends the user to Job Application, product Settings (admin, client missing), or Help (everyone else, client missing). Connect and disconnect stay on Job Application Settings. Freshness of Google data is [015-google-workspace-sync.md](015-google-workspace-sync.md).

## Consequences

Do not add module navigation to the AppBar. This control is connection status, not a feature link cluster. Anonymous chrome stays brand plus Sign in.

## Related

- [007-account-menu.md](007-account-menu.md)
- [006-google-oauth-job-application.md](006-google-oauth-job-application.md)
- [015-google-workspace-sync.md](015-google-workspace-sync.md)
