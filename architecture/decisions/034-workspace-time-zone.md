# Workspace time zone

## Context

Timestamps across Flexis used the browser default zone. Users on shared demos or remote ngrok sessions needed one explicit workspace zone, visible in the app bar and editable without opening Settings.

## Decision

Each user stores an IANA `TimeZoneId` on `users` (empty until chosen). `UserDto.timeZoneId` is returned on auth. `PUT /api/auth/me` accepts `timeZoneId` with display name and optional password. Empty means “use this device’s zone” until the user saves one.

The AppBar shows a live clock (`TimeZoneClock`) with short time and zone abbreviation. Click opens a popover: large clock, searchable zone list, quick chips (This device, UTC, common cities). Choosing a zone saves immediately. Settings → Account has the same zone field.

All displayed timestamps use `shared/time` formatters with `useTimeZone()` (resolved preferred zone or browser fallback). Relative “x mins ago” strings stay duration-based.

## Consequences

Do not format product timestamps with bare `toLocaleString()` or `Intl.DateTimeFormat` without `timeZone`. Admin user create/update does not set another user’s zone.

## Related

- [017-signed-in-account-profile.md](017-signed-in-account-profile.md)
- [007-account-menu.md](007-account-menu.md)
