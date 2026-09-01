# Header client integration status

## Context

Google Cloud and Microsoft Azure clients are saved by an admin on Settings. Without them, Connect Gmail and Connect Outlook stay blocked. That readiness lived only on Settings, Dashboard attention, and Help, so it was easy to miss from other screens. Gmail status in the AppBar reports personal mailbox connection, not whether the Flexis OAuth apps are configured.

## Decision

When signed in, the AppBar shows a client integrations control left of Google sync. It reads Google `configured` from `googleConnectionQueryKey` and Microsoft readiness from `mailCheckMailboxQueryKey` (`outlookAvailable`). The pill shows Google and Microsoft labels with live or idle orbs. A menu explains each client and sends admins to Settings or everyone else to Help. Saving either client still happens on Settings. Personal mailbox connect stays on Job Application Settings and Mail Check Settings.

## Consequences

Do not add module navigation to the AppBar. This control is OAuth app readiness, not Gmail sync or personal mailbox state. Those stay in [014-header-google-status.md](014-header-google-status.md) and [015-google-workspace-sync.md](015-google-workspace-sync.md).

## Related

- [007-account-menu.md](007-account-menu.md)
- [009-google-client-in-settings.md](009-google-client-in-settings.md)
- [021-microsoft-client-in-settings.md](021-microsoft-client-in-settings.md)
- [014-header-google-status.md](014-header-google-status.md)
