# Dashboard workspace status

## Context

Signed-in users land on Dashboard. The product already has health, Google connection, pipeline, financial, logs, and admin users APIs. Dashboard was empty, so current status lived only inside Job Application tabs.

## Decision

Dashboard is a read-only status board for the signed-in account. It composes existing query keys: health, Google connection, pipeline, financial, logs, and `users` when the role is Admin. It does not add a dashboard API. KPIs use Today (last Update batch) and Main (full profile main sheet) from the financial board. Status mix uses Ready (Download filled), Not ready (blank Download and blank Status), blank Status, Applied, and Interview; Applied / Interview / blank Status are among ready rows. Price by pipeline ranks Today price and shows Main beside it. Pipeline contribution lists Today counts plus Main price. Attention and recent activity are derived on the client. Sheet counts stay zero until Gmail can read profile workbooks, same as Financial. Google workspace sync refreshes the financial board on the regular timer so these counts update without opening Job Application.

## Consequences

Dashboard shares TanStack Query cache with Job Application and the header Google sync. New metrics must come from existing contracts or a new API, not invented client fields. Admin-only user counts stay behind `usersQueryKey` and the Admin role.

## Related

- [005-signed-in-left-nav.md](005-signed-in-left-nav.md)
- [013-job-application-financial-logs.md](013-job-application-financial-logs.md)
- [015-google-workspace-sync.md](015-google-workspace-sync.md)
