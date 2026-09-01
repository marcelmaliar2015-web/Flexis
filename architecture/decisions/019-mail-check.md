# Mail Check mailbox and OpenAI

## Context

Job seekers need interview mail kept and application receipts discarded. Mail Check needs its own mailbox connection, separate from Job Application Gmail used for sheets and pipeline. Classification needs a model; the user pays only for an OpenAI API key.

## Decision

Mail Check is a signed-in module under `/mail-check` with Inbox, Check, and Settings tabs. It stores per-user mailbox connections in `mail_connections` (any number of Gmail and Outlook accounts; see [023-multi-mailbox-mail-check.md](023-multi-mailbox-mail-check.md)). Gmail and Outlook connect and disconnect live on Mail Check Settings. Job Application Gmail stays on Job Application Settings for Sheets and Drive.

Gmail OAuth uses `openid`, `userinfo.email`, and `gmail.modify`. Outlook OAuth uses Microsoft identity with `Mail.ReadWrite` and `MailboxSettings.ReadWrite` through Microsoft Graph. Outlook requires an admin to save the Microsoft client on Settings. The API returns `outlookAvailable: true` when that client is saved and `Microsoft:RedirectUri` is set. Microsoft OAuth HTTP calls use a 60 second timeout.

Gmail uses labels and stars. Outlook uses master categories and flagged messages. Both create five keep groups: Interview Schedule, Availability Request, Assessment Request, HR Team Message, and Reply required.

On first use Flexis creates those groups on each connected mailbox. A check reads inbox and junk (Outlook) or inbox, spam, and category tabs (Gmail). It does not scan sent, drafts, or archive. Check all covers every connected mailbox; Check covers one. Each API run classifies one new message per mailbox. Already processed ids are skipped. Candidate scans resume past already-checked ids so large inboxes keep moving forward. Check all keeps requesting runs until those folders are caught up.

The user's OpenAI key classifies each new message with a configurable prompt on Settings. The model must return JSON with criteria flags (`job_application_related`, `action`, `message_type`, `needs_reply`, `draft_reply`, `reason`). Non-job mail is left untouched. Job application noise is trashed. Interview Schedule, Availability Request, Assessment Request, and HR Team Message mail is labeled or categorized and pinned. When a reply is required, Flexis saves a draft reply in the mailbox and does not send it. Processed message ids are stored per mailbox so Flexis does not pay to classify them again.

Auto-check runs about every two minutes while the browser tab is visible, only when at least one mailbox is connected and a key is saved.

## Consequences

Do not reuse Job Application `google_connections` for Mail Check. Do not store the OpenAI key in the browser. Do not trash or relabel non-job mail. Do not send reply drafts automatically. New product modules still add a left-nav item and a route under `AuthenticatedLayout`.
