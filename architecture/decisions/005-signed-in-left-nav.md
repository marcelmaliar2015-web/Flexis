# Signed-in left navigation

## Context

Flexis is a multi-module product. After sign-in the user needs one place to switch among modules.

## Decision

Authenticated product screens render in `AuthenticatedLayout` with a left panel of module buttons. The first modules are Dashboard, Job Application, and Settings. Sign-in lands on Dashboard. Public landing and sign-in stay outside this shell.

## Consequences

New product modules add a left-nav item and a feature route under `RequireAuth` and `AuthenticatedLayout`. Do not put public marketing screens in this shell.
