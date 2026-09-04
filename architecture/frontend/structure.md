# Frontend structure

## Directory map

```
frontend/src/
  app/           shell, providers, router, layout
  features/      one folder per feature
    home/
    health/
    help/
    auth/
    dashboard/
    jobApplication/
    mailCheck/
    logs/
    settings/
  shared/
    api/
    auth/
    config/
    types/
    notifications/
```

Path alias `@/` maps to `frontend/src/`.

## Modules

| Module | Responsibility |
| --- | --- |
| `app` | Bootstrap, theme, query client, routes, layout, guards, account menu, session lifecycle, header Issues, header Mail Check auto-check, header client integrations, header Gmail status, Google workspace sync, Mail Check auto-check provider |
| `features/home` | Home screen |
| `features/health` | Health screen and query hook |
| `features/help` | Tabbed product guides. Overview is the product map; Google setup, Operations, Financial, Logs, Mail Check, and Problems are topic guides. |
| `features/auth` | Sign-in screen |
| `features/dashboard` | Workspace status board (health, Google, pipeline, financial, logs, Admin users) |
| `features/jobApplication` | Job Application tabs: Operations, Financial, Resume generation. Pipeline table, pipeline bulk progress, Financial summary cards, financial performance chart, pipeline entry detail. Catalog and Gmail connect UI live on Settings (Job Application tab). Activity log UI is hosted on Logs |
| `features/mailCheck` | Mail Check tabs: Need action, Inbox, Check, Usage (OpenAI cost chart). Mailbox, OpenAI, label, and prompt settings live on Settings (Mail Check tab). Action log UI is hosted on Logs |
| `features/logs` | Logs screen with tabs: Job Application and Mail Check activity (reuses feature log components) |
| `features/settings` | Settings screen with tabs: Account, Job Application, Mail Check, Admin (Google Cloud client, Microsoft client, users) |
| `shared/api` | `fetch` wrapper and endpoint functions |
| `shared/auth` | Session provider and token storage |
| `shared/config` | Env access and `appPaths` |
| `shared/types` | Shared TypeScript contracts |
| `shared/notifications` | Issue notice store used by the API client and the header Issues list |

## Import rules

- `app` may import `features` and `shared`
- `features` may import `shared` and files in the same feature
- `shared` may not import `app` or `features`

## Related

- [overview.md](overview.md)
