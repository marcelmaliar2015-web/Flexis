# Signed-in account profile

## Context

The account menu listed name, email, and role as a text block. Settings showed the signed-in person inside the Admin users table, so they edited themselves like any other directory row. User and Viewer had no Settings form.

## Decision

`UserMenu` uses a compact header: avatar, display name, email, and a role chip, then Settings, Help, and Sign out. Settings always has a Your account card. The signed-in user can update display name and optional password. Email and role are read-only. `PUT /api/auth/me` performs that update. The Admin users table lists other accounts only.

## Consequences

Do not edit the signed-in user through the users table. Role and active stay Admin-only on other users via `PUT /api/users/{id}`.

## Related

- [004-jwt-role-users.md](004-jwt-role-users.md)
- [007-account-menu.md](007-account-menu.md)
