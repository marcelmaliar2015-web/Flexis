# Clean architecture backend

## Context

Flexis will grow many features. A single ASP.NET project would mix HTTP, rules, and persistence.

## Decision

Use four projects: Domain, Application, Infrastructure, Api. Dependencies point inward. Host composition lives in `Program.cs` via `AddApplication` and `AddInfrastructure`.

## Consequences

New features add types in these layers. Do not add a new project unless a bounded context needs isolation. Controllers stay thin.
