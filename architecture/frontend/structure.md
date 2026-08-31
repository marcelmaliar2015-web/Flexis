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
| `app` | Bootstrap, theme, query client, routes, layout, guards, account menu |
| `features/home` | Home screen |
| `features/health` | Health screen and query hook |
| `features/help` | In-app Gmail connect guide |
| `features/auth` | Sign-in screen |
| `features/dashboard` | Empty Dashboard screen |
| `features/jobApplication` | Job Application tabs, Pipeline table, pipeline entry detail, Settings (Gmail, profiles, sources, locations) |
| `features/settings` | Settings screen; Admin Google Cloud client and user management |
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
