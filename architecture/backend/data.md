# Backend data

## Persistence

Two stores, registered in `Flexis.Infrastructure.DependencyInjection`.

| Store | Use | Access |
| --- | --- | --- |
| PostgreSQL | Relational data, users, Google connections, Google Cloud client, Microsoft client, job catalog items, pipeline entries, banned companies, financial settings, activity logs, Mail Check settings and processed messages | EF Core `FlexisDbContext`, connection `ConnectionStrings:Postgres` |
| MongoDB | Document data | `IMongoClient` singleton, `IMongoDatabase` named `Mongo:Database` |

Local containers: `docker-compose.yml` (user `flexis`, password `flexis`, database `flexis`).

## Entities

`User` in `Flexis.Domain.Users`. Table `users`, unique email. EF configuration: `Persistence/Postgres/Users/UserConfiguration.cs`.

`GoogleConnection` in `Flexis.Domain.Google`. Table `google_connections`, unique `UserId`, cascade from `users`. EF configuration: `Persistence/Postgres/Google/GoogleConnectionConfiguration.cs`. Refresh and access tokens are stored protected, not as plaintext. Drive folder IDs for `Flexis`, `Job Application`, `Profiles`, and `Sources` are stored on the same row.

`GoogleClientCredentials` in `Flexis.Domain.Google`. Table `google_client_credentials`. One Flexis Google Cloud web client. The secret is stored protected. EF configuration: `Persistence/Postgres/Google/GoogleClientCredentialsConfiguration.cs`.

`MicrosoftClientCredentials` in `Flexis.Domain.Microsoft`. Table `microsoft_client_credentials`. One Flexis Azure app for Mail Check Outlook. The secret is stored protected. EF configuration: `Persistence/Postgres/Microsoft/MicrosoftClientCredentialsConfiguration.cs`.

`JobCatalogItem` in `Flexis.Domain.JobApplication`. Table `job_catalog_items`. Kind is `Profile` or `Source`. Unique (`UserId`, `Kind`, `Title`), cascade from `users`. `SpreadsheetId` and `Url` store the Google Sheet Flexis created. EF configuration: `Persistence/Postgres/JobApplication/JobCatalogItemConfiguration.cs`. `createdAt` is set on create and is not editable. Source locations live as tabs in that spreadsheet, not as rows.

`JobPipelineEntry` in `Flexis.Domain.JobApplication`. Table `job_pipeline_entries`. Unique (`UserId`, `ProfileId`, `SourceId`, `LocationSheetId`), cascade from `users`. `ApplyRate` and `BonusRate` are per row. EF configuration: `Persistence/Postgres/JobApplication/JobPipelineEntryConfiguration.cs`.

`JobPipelineBannedCompany` in `Flexis.Domain.JobApplication`. Table `job_pipeline_banned_companies`. Unique (`PipelineEntryId`, `MatchKey`), cascade from `job_pipeline_entries`. EF configuration: `Persistence/Postgres/JobApplication/JobPipelineBannedCompanyConfiguration.cs`.

`JobFinancialSettings` in `Flexis.Domain.JobApplication`. Table `job_financial_settings`. Unique `UserId`, cascade from `users`. Default apply rate 0.06 and bonus rate 1.5. EF configuration: `Persistence/Postgres/JobApplication/JobFinancialSettingsConfiguration.cs`.

`JobResumeSettings` in `Flexis.Domain.JobApplication`. Table `job_resume_settings`. Unique `UserId`, cascade from `users`. Stores owner option list JSON and job-master spreadsheet id and url. EF configuration: `Persistence/Postgres/JobApplication/JobResumeSettingsConfiguration.cs`.

`JobProfileResumeSettings` in `Flexis.Domain.JobApplication`. Table `job_profile_resume_settings`. Unique `ProfileId`, cascade from `job_catalog_items`. Per-profile prompt, resume style, and owner. EF configuration: `Persistence/Postgres/JobApplication/JobProfileResumeSettingsConfiguration.cs`.

`JobApplicationLog` in `Flexis.Domain.JobApplication`. Table `job_application_logs`. Cascade from `users`. Newest 200 rows are listed per user. EF configuration: `Persistence/Postgres/JobApplication/JobApplicationLogConfiguration.cs`.

`MailCheckSettings` in `Flexis.Domain.MailCheck`. Table `mail_check_settings`. Unique `UserId`, cascade from `users`. OpenAI key stored protected. Default model `gpt-4o-mini`. Optional `ClassifierPrompt` text; empty uses the built-in default. EF configuration: `Persistence/Postgres/MailCheck/MailCheckSettingsConfiguration.cs`.

`MailConnection` in `Flexis.Domain.MailCheck`. Table `mail_connections`. Unique (`UserId`, `Provider`, `ExternalSubject`), cascade from `users`. Provider is `Gmail` or `Outlook`. Refresh and access tokens are stored protected. EF configuration: `Persistence/Postgres/MailCheck/MailConnectionConfiguration.cs`.

`MailCheckProcessedMessage` in `Flexis.Domain.MailCheck`. Table `mail_check_processed_messages`. Unique (`MailConnectionId`, `MessageId`), cascade from `users` and from `mail_connections`. EF configuration: `Persistence/Postgres/MailCheck/MailCheckProcessedMessageConfiguration.cs`.

## Migrations

EF Core migrations in `Flexis.Infrastructure`. In Development the API runs `PostgresStartup.InitializeAsync` (migrate, then seed users if the table is empty).

## Related

- [overview.md](overview.md)
- [structure.md](structure.md)
