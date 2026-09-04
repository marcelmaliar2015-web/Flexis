# Mail Check OpenAI usage hours

## Context

Mail Check spends the user's OpenAI key on classify calls. Users need to see token volume and estimated cost over time with hourly and daily views, similar to Financial performance snapshots.

## Decision

Each successful classify records OpenAI `usage` tokens into `mail_check_usage_hours` (one row per user per UTC hour). Estimated USD cost uses published list rates in `OpenAiTokenPricing` for common models, matching the longest model-id prefix (so `gpt-5.6-luna` does not inherit `gpt-5` rates). `GET /api/mail-check/usage` returns lifetime and today totals plus up to 14 days of hourly history. The Mail Check Usage tab charts estimated cost, tokens, and call count with Hourly and Daily toggles (daily sums hours in each day).

## Consequences

History starts when classify runs after this change. Failed classify attempts that never return usage are not recorded. Estimates can differ from OpenAI invoices.
