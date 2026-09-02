# Issue notifications

## Context

Errors were shown only on the screen that made the request, or swallowed by auto-check. That made it hard to see the exact failure and fix it later.

## Decision

Every API failure, Mail Check classification error, unhandled browser error, and UI render crash is recorded as an issue. The header Issues control opens the list. A snackbar shows the newest item. Each item keeps time, severity, source, message, HTTP method, path, and status. Copy all produces that log. The API writes the same facts to `.flexis/issue-log.jsonl` at the repo root (gitignored). Unexpected server exceptions also store the exception text in that file. Development 500 responses use the exception message. `401` responses are not notified; the API client clears the session and React Query cache so the user is sent to Sign in.

`POST /api/diagnostics/events` records client-only issues (network, Mail Check item failures, window errors) when a session exists. API exceptions are written by the exception handler and are not posted again.

Page components do not repeat API or network failures in inline alerts. Use `userFacingError` for form handlers. The Issues drawer and snackbar are the only place for failed request copy.

## Consequences

Do not log request bodies, tokens, or the OpenAI key. Do not notify health `503` payloads. New failure paths should call `reportIssue` or throw through the API client.
