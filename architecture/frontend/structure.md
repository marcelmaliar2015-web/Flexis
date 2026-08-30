# Frontend structure

## Directory map

```
frontend/src/
  app/           shell, providers, router, layout
  features/      one folder per feature
    home/
    health/
  shared/
    api/
    config/
    types/
```

Path alias `@/` maps to `frontend/src/`.

## Modules

| Module | Responsibility |
| --- | --- |
| `app` | Bootstrap, theme, query client, routes, layout |
| `features/home` | Home screen |
| `features/health` | Health screen and query hook |
| `shared/api` | `fetch` wrapper and endpoint functions |
| `shared/config` | Env access |
| `shared/types` | Shared TypeScript contracts |

## Import rules

- `app` may import `features` and `shared`
- `features` may import `shared` and files in the same feature
- `shared` may not import `app` or `features`

## Related

- [overview.md](overview.md)
