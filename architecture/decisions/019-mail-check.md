# Mail Check Gmail and OpenAI

## Context

Job seekers need interview mail kept and application receipts discarded. Gmail is already connected for Job Application with `gmail.modify`. Classification needs a model; the user pays only for an OpenAI API key.

## Decision

Mail Check is a signed-in module under `/mail-check` with Inbox, Check, and Settings tabs. It reuses the existing Gmail OAuth connection. Connect and disconnect stay on Job Application Settings.

On first use Flexis creates four Gmail labels: Interview Scheduled, Waiting for answer, Need to Schedule/Availability, and Others. A check reads inbox, spam, promotions, updates, forums, and social. The user's OpenAI key classifies each new message. Keepers are labeled and starred (Gmail pin). Spam keepers are moved to the inbox. Application noise is moved to trash. Personal mail is skipped and not modified. Processed Gmail ids are stored so Flexis does not pay to classify them again.

The OpenAI key is AES-GCM protected with `Google:TokenProtectionKey`. The key is never returned. The user picks any chat or reasoning model; the API client adapts `max_tokens` / `max_completion_tokens`, temperature, JSON format, system vs user messages, and falls back from Chat Completions to Responses.

Auto-check runs about every two minutes while the browser tab is visible, only when Gmail is connected and a key is saved.

## Consequences

Do not request extra Gmail or Drive scopes for Mail Check. Do not store the OpenAI key in the browser. Do not trash non-job mail. New product modules still add a left-nav item and a route under `AuthenticatedLayout`.
