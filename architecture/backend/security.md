# Backend security

## Authentication

None yet.

## Authorization

None yet. CORS policy `Frontend` allows `Frontend:Origins` (`http://localhost:5173` and `http://127.0.0.1:5173` in Development).

## Secrets and transport

Development uses HTTP on port 5080 and local Compose credentials in `appsettings.Development.json`. Production secrets must come from environment or a secret store, not committed files.

## Related

- [overview.md](overview.md)
- [api.md](api.md)
- [../frontend/routing.md](../frontend/routing.md)
