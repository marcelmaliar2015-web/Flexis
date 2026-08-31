# Account menu in the app bar

## Context

Signed-in chrome had Home, Health, Users, display name, and Sign out in the AppBar. Those items competed with the left nav and put admin user management in the top bar.

## Decision

The AppBar shows the Flexis brand, and when signed in a Gmail status control then an account avatar (`UserMenu`), separated by a hairline. The menu header is avatar, display name, email, and a role chip, then Settings, Help, and Sign out. Account edits are on Settings, not in the menu. Module navigation stays in the left nav. Admin user management lives on Settings and excludes the signed-in user. Help is a product route with tabbed guides, including Google setup. Gmail status in the header is [014-header-google-status.md](014-header-google-status.md). Signed-in account profile is [017-signed-in-account-profile.md](017-signed-in-account-profile.md).

## Consequences

Do not add module links to the AppBar. Account actions go in `UserMenu`. Product modules go in `AuthenticatedLayout`. Gmail connection status is allowed in the AppBar per [014-header-google-status.md](014-header-google-status.md).
