# Multi-mailbox Mail Check

## Context

Mail Check stored one `mail_connections` row per user. Connect Outlook was disabled while Gmail was connected, and users could not triage more than one inbox.

## Decision

A signed-in user may connect any number of Gmail and Outlook mailboxes. Unique key is `(UserId, Provider, ExternalSubject)`. Reconnecting the same account refreshes tokens. Disconnect is per connection id. Check all and auto-check process every connected mailbox. Inbox is a combined list with mailbox identity on each row. Processed message ids are scoped by `MailConnectionId`. OpenAI settings stay one per user. Outlook connect still requires the admin Microsoft client and `Microsoft:RedirectUri` (`outlookAvailable`).

Supersedes the single-mailbox wording in [019-mail-check.md](019-mail-check.md).

## Consequences

Do not block Add Outlook when another mailbox is already connected. Do not store one shared processed-message namespace across mailboxes. Job Application Gmail remains a separate connection.

## Related

- [019-mail-check.md](019-mail-check.md)
- [021-microsoft-client-in-settings.md](021-microsoft-client-in-settings.md)
