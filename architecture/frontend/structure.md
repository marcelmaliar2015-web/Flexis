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
    settings/
  shared/
    api/
    auth/
    config/
    types/
```

Path alias `@/` maps to `frontend/src/`.

## Modules

| Module | Responsibility |
| --- | --- |
| `app` | Bootstrap, theme, query client, routes, layout, guards, account menu, header Gmail status, Google workspace sync |
| `features/home` | Home screen |
| `features/health` | Health screen and query hook |
| `features/help` | Tabbed product guides (Overview, Google setup, Operations, Financial, Logs, Problems) |
| `features/auth` | Sign-in screen |
| `features/dashboard` | Workspace status board (health, Google, pipeline, financial, logs, Admin users) |
| `features/jobApplication` | Job Application tabs, Pipeline table, pipeline entry detail, Financial, Logs, Settings (Gmail, default rates, profiles, sources, locations) |
| `features/settings` | Settings screen; signed-in account profile; Admin Google Cloud client and other users |
| `shared/api` | `fetch` wrapper and endpoint functions |
| `shared/auth` | Session provider and token storage |
| `shared/config` | Env access and `appPaths` |
| `shared/types` | Shared TypeScript contracts |

## Import rules

- `app` may import `features` and `shared`
- `features` may import `shared` and files in the same feature
- `shared` may not import `app` or `features`

## Related

- [overview.md](overview.md)
