# Backend structure

## Directory map

```
backend/src/
  Flexis.Api/
    Controllers/
    Properties/
  Flexis.Application/
    Health/
  Flexis.Domain/
  Flexis.Infrastructure/
    Persistence/Postgres/
    Persistence/Mongo/
```

Shared MSBuild settings: `backend/Directory.Build.props` (`net10.0`, nullable, warnings as errors, NuGet audit on direct packages).

## Modules

| Project | Responsibility |
| --- | --- |
| Flexis.Domain | Domain model |
| Flexis.Application | Application services and API contracts |
| Flexis.Infrastructure | PostgreSQL, MongoDB, health checks |
| Flexis.Api | Host, CORS, controllers |

## Layers

Clean architecture. Controllers call application types and framework services. Persistence types stay in Infrastructure. New features add types inside the matching layer, not a fifth project, until a bounded context needs its own assembly.

## Related

- [overview.md](overview.md)
