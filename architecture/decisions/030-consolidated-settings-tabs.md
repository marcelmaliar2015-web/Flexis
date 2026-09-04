# Consolidated Settings page tabs

## Context

Settings were split across product Settings, Job Application Settings, and Mail Check Settings. Users had to hunt across modules for Gmail, mailboxes, OpenAI, and admin clients.

## Decision

`/settings` is the only settings surface. It uses MUI tabs: Account, Job Application, Mail Check, and Admin (Admin role only). Job Application and Mail Check reuse their existing settings tab components. Module pages no longer have a Settings tab. Optional `?tab=` deep-links the active Settings tab. Job Application Gmail and Mail Check mailbox OAuth `returnUrl` may be `/settings` (legacy `/job-application` and `/mail-check` still accepted); callbacks land on Settings with `?google=` or `?mailbox=` and open the matching tab.

## Consequences

Help, dashboard attention, and header copy point to Settings tabs by name. Keep feature-owned components under `features/jobApplication` and `features/mailCheck`; Settings only hosts them.
