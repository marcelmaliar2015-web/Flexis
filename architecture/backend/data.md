# Backend data

## Persistence

Two stores, registered in `Flexis.Infrastructure.DependencyInjection`.

| Store | Use | Access |
| --- | --- | --- |
| PostgreSQL | Relational data, users, Google connections, Google Cloud client, job catalog items, pipeline entries, banned companies | EF Core `FlexisDbContext`, connection `ConnectionStrings:Postgres` |
| MongoDB | Document data | `IMongoClient` singleton, `IMongoDatabase` named `Mongo:Database` |

Local containers: `docker-compose.yml` (user `flexis`, password `flexis`, database `flexis`).

## Entities

`User` in `Flexis.Domain.Users`. Table `users`, unique email. EF configuration: `Persistence/Postgres/Users/UserConfiguration.cs`.

`GoogleConnection` in `Flexis.Domain.Google`. Table `google_connections`, unique `UserId`, cascade from `users`. EF configuration: `Persistence/Postgres/Google/GoogleConnectionConfiguration.cs`. Refresh and access tokens are stored protected, not as plaintext. Drive folder IDs for `Flexis`, `Job Application`, `Profiles`, and `Sources` are stored on the same row.

`GoogleClientCredentials` in `Flexis.Domain.Google`. Table `google_client_credentials`. One Flexis Google Cloud web client. The secret is stored protected. EF configuration: `Persistence/Postgres/Google/GoogleClientCredentialsConfiguration.cs`.

`JobCatalogItem` in `Flexis.Domain.JobApplication`. Table `job_catalog_items`. Kind is `Profile` or `Source`. Unique (`UserId`, `Kind`, `Title`), cascade from `users`. `SpreadsheetId` and `Url` store the Google Sheet Flexis created. EF configuration: `Persistence/Postgres/JobApplication/JobCatalogItemConfiguration.cs`. `createdAt` is set on create and is not editable. Source locations live as tabs in that spreadsheet, not as rows.

`JobPipelineEntry` in `Flexis.Domain.JobApplication`. Table `job_pipeline_entries`. Unique (`UserId`, `ProfileId`, `SourceId`, `LocationSheetId`), cascade from `users`. EF configuration: `Persistence/Postgres/JobApplication/JobPipelineEntryConfiguration.cs`.

`JobPipelineBannedCompany` in `Flexis.Domain.JobApplication`. Table `job_pipeline_banned_companies`. Unique (`PipelineEntryId`, `MatchKey`), cascade from `job_pipeline_entries`. EF configuration: `Persistence/Postgres/JobApplication/JobPipelineBannedCompanyConfiguration.cs`.

## Migrations

EF Core migrations in `Flexis.Infrastructure`. In Development the API runs `PostgresStartup.InitializeAsync` (migrate, then seed users if the table is empty).

## Related

- [overview.md](overview.md)
- [structure.md](structure.md)
