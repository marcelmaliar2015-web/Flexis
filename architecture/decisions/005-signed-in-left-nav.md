# Signed-in left navigation

## Context

Flexis is a multi-module product. After sign-in the user needs one place to switch among modules.

## Decision

Authenticated product screens render in `AuthenticatedLayout` with a left panel of module buttons. The modules are Dashboard, Job Application, Mail Check, Settings, and Help. Sign-in lands on Dashboard. Public landing and sign-in stay outside this shell and are guest-only. The left panel stays in view; only the module content pane scrolls.

## Consequences

New product modules add a left-nav item and a feature route under `RequireAuth` and `AuthenticatedLayout`. Do not put public marketing screens in this shell. Account actions stay in `UserMenu`, not the left nav.
