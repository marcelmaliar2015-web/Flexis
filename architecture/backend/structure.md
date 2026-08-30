# Backend structure

## Directory map

```
backend/src/
  Flexis.Api/
    Controllers/
    Properties/
  Flexis.Application/
    Auth/
    Common/
    Health/
    Users/
  Flexis.Domain/
    Users/
  Flexis.Infrastructure/
    Persistence/Postgres/
    Persistence/Postgres/Users/
    Persistence/Mongo/
    Security/
```

Shared MSBuild settings: `backend/Directory.Build.props` (`net10.0`, nullable, warnings as errors, NuGet audit on direct packages).

## Modules

| Project | Responsibility |
| --- | --- |
| Flexis.Domain | Domain model |
| Flexis.Application | Application services and API contracts |
| Flexis.Infrastructure | PostgreSQL, MongoDB, JWT, password hashing, health checks |
| Flexis.Api | Host, CORS, JWT bearer, controllers |

## Layers

Clean architecture. Controllers call application types and framework services. Persistence types stay in Infrastructure. New features add types inside the matching layer, not a fifth project, until a bounded context needs its own assembly.

## Related

- [overview.md](overview.md)
