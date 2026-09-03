# Mail Check action log

## Context

Mail Check only kept session-local “Recent decisions” on Check and a dedupe table without subject, action, source, or duration. Auto-check and manual check history could not be audited at volume.

## Decision

Persist every classified or enforced message action and each server round summary in `mail_check_action_logs`. Each row stores source (`auto` or `manual`), run id, mailbox, message id, subject, from, action, label, detail, and duration. `GET /api/mail-check/logs` returns a server-paged list with source, action, mailbox, and text filters. The Mail Check UI adds a Log tab with sticky table, chips, search, and pagination.

## Consequences

Job Application’s newest-200 activity feed stays as-is. Mail Check log volume uses page size up to 100. Disconnecting a mailbox nulls `MailConnectionId` on existing log rows and keeps the email snapshot. Help and architecture list Log beside Check and Settings.
