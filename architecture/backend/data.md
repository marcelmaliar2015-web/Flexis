# Backend data

## Persistence

Two stores, registered in `Flexis.Infrastructure.DependencyInjection`.

| Store | Use | Access |
| --- | --- | --- |
| PostgreSQL | Relational data, users, Google connections, job catalog items | EF Core `FlexisDbContext`, connection `ConnectionStrings:Postgres` |
| MongoDB | Document data | `IMongoClient` singleton, `IMongoDatabase` named `Mongo:Database` |

Local containers: `docker-compose.yml` (user `flexis`, password `flexis`, database `flexis`).

## Entities

`User` in `Flexis.Domain.Users`. Table `users`, unique email. EF configuration: `Persistence/Postgres/Users/UserConfiguration.cs`.

`GoogleConnection` in `Flexis.Domain.Google`. Table `google_connections`, unique `UserId`, cascade from `users`. EF configuration: `Persistence/Postgres/Google/GoogleConnectionConfiguration.cs`. Refresh and access tokens are stored protected, not as plaintext.

`JobCatalogItem` in `Flexis.Domain.JobApplication`. Table `job_catalog_items`. Kind is `Profile` or `Source`. Unique (`UserId`, `Kind`, `Title`), cascade from `users`. EF configuration: `Persistence/Postgres/JobApplication/JobCatalogItemConfiguration.cs`. `createdAt` is set on create and is not editable.

## Migrations

EF Core migrations in `Flexis.Infrastructure`. In Development the API runs `PostgresStartup.InitializeAsync` (migrate, then seed users if the table is empty).

## Related

- [overview.md](overview.md)
- [structure.md](structure.md)
