# Backend data

## Persistence

Two stores, registered in `Flexis.Infrastructure.DependencyInjection`.

| Store | Use | Access |
| --- | --- | --- |
| PostgreSQL | Relational data, users | EF Core `FlexisDbContext`, connection `ConnectionStrings:Postgres` |
| MongoDB | Document data | `IMongoClient` singleton, `IMongoDatabase` named `Mongo:Database` |

Local containers: `docker-compose.yml` (user `flexis`, password `flexis`, database `flexis`).

## Entities

`User` in `Flexis.Domain.Users`. Table `users`, unique email. EF configuration: `Persistence/Postgres/Users/UserConfiguration.cs`.

## Migrations

EF Core migrations in `Flexis.Infrastructure`. In Development the API runs `PostgresStartup.InitializeAsync` (migrate, then seed users if the table is empty).

## Related

- [overview.md](overview.md)
- [structure.md](structure.md)
