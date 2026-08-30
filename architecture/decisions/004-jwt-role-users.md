# JWT roles and user management

## Context

Flexis needs sign-in and three roles. Users are relational accounts, not documents.

## Decision

Store users in PostgreSQL. Issue JWT access tokens. Roles are `Admin`, `User`, and `Viewer`. Custom `User` in Domain; password hashing and JWT issuance in Infrastructure. Do not adopt ASP.NET Identity UI or IdentityDbContext.

Admin can list, create, and update users of every role. The last active admin cannot be demoted or deactivated. `GET /api/health` stays anonymous. Other API endpoints require a valid token. User and Viewer currently share the same screens; User is the future write role.

## Consequences

New protected features use `[Authorize]` and route guards. Session lives in `shared/auth`. Do not add a client store for auth.
