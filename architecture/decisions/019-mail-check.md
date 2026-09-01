# Mail Check mailbox and OpenAI

## Context

Job seekers need interview mail kept and application receipts discarded. Mail Check needs its own mailbox connection, separate from Job Application Gmail used for sheets and pipeline. Classification needs a model; the user pays only for an OpenAI API key.

## Decision

Mail Check is a signed-in module under `/mail-check` with Inbox, Check, and Settings tabs. It stores per-user mailbox connections in `mail_connections` (any number of Gmail and Outlook accounts; see [023-multi-mailbox-mail-check.md](023-multi-mailbox-mail-check.md)). Gmail and Outlook connect and disconnect live on Mail Check Settings. Job Application Gmail stays on Job Application Settings for Sheets and Drive.

Gmail OAuth uses `openid`, `userinfo.email`, and `gmail.modify`. Outlook OAuth uses Microsoft identity with `Mail.ReadWrite` and `MailboxSettings.ReadWrite` through Microsoft Graph. Outlook requires an admin to save the Microsoft client on Settings. The API returns `outlookAvailable: true` when that client is saved and `Microsoft:RedirectUri` is set.

Gmail uses labels and stars. Outlook uses master categories and flagged messages. Both create the same four keep groups: Interview Scheduled, Waiting for answer, Need to Schedule/Availability, and Others.

On first use Flexis creates those groups on each connected mailbox. A check reads inbox and junk (Outlook) or inbox, spam, and category tabs (Gmail) on every connected mailbox. It does not scan sent, drafts, or archive. Check all keeps requesting batches until those candidate folders are caught up across all mailboxes. Each API batch classifies a limited number of new messages; already processed ids are skipped per mailbox. The user's OpenAI key classifies each new message. Keepers are labeled or categorized and pinned. Junk or spam keepers are moved to the inbox. Application noise is moved to trash. Personal mail is skipped and not modified. Processed message ids are stored per mailbox so Flexis does not pay to classify them again.

Auto-check runs about every two minutes while the browser tab is visible, only when at least one mailbox is connected and a key is saved.

## Consequences

Do not reuse Job Application `google_connections` for Mail Check. Do not store the OpenAI key in the browser. Do not trash non-job mail. New product modules still add a left-nav item and a route under `AuthenticatedLayout`.
