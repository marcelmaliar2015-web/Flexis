# Mail Check mailbox and OpenAI

## Context

Job seekers need interview mail kept and application receipts discarded. Mail Check needs its own mailbox connection, separate from Job Application Gmail used for sheets and pipeline. Classification needs a model; the user pays only for an OpenAI API key.

## Decision

Mail Check is a signed-in module under `/mail-check` with Need action, Inbox, Check, and Settings tabs. It stores per-user mailbox connections in `mail_connections` (any number of Gmail and Outlook accounts; see [023-multi-mailbox-mail-check.md](023-multi-mailbox-mail-check.md)). Gmail and Outlook connect and disconnect live on Mail Check Settings. Job Application Gmail stays on Job Application Settings for Sheets and Drive.

Gmail OAuth uses `openid`, `userinfo.email`, and `gmail.modify`. Outlook OAuth uses Microsoft identity with `User.Read`, `Mail.ReadWrite`, and `MailboxSettings.ReadWrite` through Microsoft Graph. Outlook requires an admin to save the Microsoft client on Settings. The API returns `outlookAvailable: true` when that client is saved and `Microsoft:RedirectUri` is set. Microsoft OAuth HTTP calls use a 60 second timeout.

Gmail uses labels and stars. Flexis creates classifier labels under the `Flexis/` namespace in Gmail (for example `Flexis/Scheduled`) so names do not collide with Gmail system labels. Outlook uses master categories with the same `Flexis/` names. Pin on Gmail also stars the message. Pin on Outlook only sets the category; it does not flag the message. Trash moves it to trash. Keep leaves the message in place. Every classified message gets its classifier label or category before trash or keep runs.

On first use Flexis creates pin labels on each connected mailbox. A check reads inbox and junk (Outlook) or inbox, spam, and category tabs (Gmail). It does not scan sent, drafts, or archive. Check all covers every connected mailbox; Check covers one. Each API run classifies one new message per mailbox. Already processed ids are skipped. Candidate scans resume past already-checked ids so large inboxes keep moving forward. Check all keeps requesting runs until those folders are caught up.

The user's OpenAI key classifies each new message with a configurable prompt on Settings. The model must return JSON with one `label` field: `rejected`, `applied`, `schedule`, `scheduled`, `assessment`, `availability`, `success`, or `other`. Settings also store per-label mailbox actions: `pin`, `trash`, or `keep`, and which labels appear on the Need action tab (default schedule, assessment, availability). Pin labels and stars or flags the message. Trash moves it to trash. Keep leaves the message untouched. Processed message ids and classified labels are stored per mailbox so Flexis does not pay to classify them again.

Auto-check runs every 20 seconds while the Mail Check page is visible when `autoCheckEnabled` is true, at least one mailbox is connected, and an OpenAI key is saved. Each run classifies one new message per mailbox to stay within free-tier Gmail, Outlook, and OpenAI limits. Manual Check on the Check tab always uses `force: true` and works when auto-check is off. Per-mailbox `checkedUntilAt` and `scanCaughtUp` show scan progress on Settings and Check.

## Consequences

Do not reuse Job Application `google_connections` for Mail Check. Do not store the OpenAI key in the browser. New product modules still add a left-nav item and a route under `AuthenticatedLayout`.
