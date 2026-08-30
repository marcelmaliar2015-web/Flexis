# Account menu in the app bar

## Context

Signed-in chrome had Home, Health, Users, display name, and Sign out in the AppBar. Those items competed with the left nav and put admin user management in the top bar.

## Decision

The AppBar shows only the Flexis brand and an account avatar (`UserMenu`). The menu lists identity (name, email, role), then Settings, Help, and Sign out. Module navigation stays in the left nav. Admin user management lives on Settings. Help is a product route with the Gmail connect guide.

## Consequences

Do not add feature links to the AppBar. Account actions go in `UserMenu`. Product modules go in `AuthenticatedLayout`.
