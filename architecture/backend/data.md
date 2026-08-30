# Backend data

## Persistence

Two stores, registered in `Flexis.Infrastructure.DependencyInjection`.

| Store | Use | Access |
| --- | --- | --- |
| PostgreSQL | Relational data | EF Core `FlexisDbContext`, connection `ConnectionStrings:Postgres` |
| MongoDB | Document data | `IMongoClient` singleton, `IMongoDatabase` named `Mongo:Database` |

Local containers: `docker-compose.yml` (user `flexis`, password `flexis`, database `flexis`).

## Entities

None yet. `FlexisDbContext` applies configurations from its assembly. Add EF configurations next to new entities. Add Mongo collections in Infrastructure when the first document type exists.

## Migrations

None yet. Add EF migrations in Infrastructure when the first relational entity is introduced.

## Related

- [overview.md](overview.md)
- [structure.md](structure.md)
