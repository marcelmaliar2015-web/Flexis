# Architecture

Source of truth for how Flexis is built. Keep every file in this folder aligned with the code.

A new session should read this index, then `overview.md`, then the frontend or backend folder that matches the work.

## Map

| File | Owns |
| --- | --- |
| [overview.md](overview.md) | Product, system context, tech stack, repo layout |
| [conventions.md](conventions.md) | Naming, errors, env, shared patterns |
| [frontend/overview.md](frontend/overview.md) | Frontend role, stack, entry, boundaries |
| [frontend/structure.md](frontend/structure.md) | Folders, modules, what may import what |
| [frontend/routing.md](frontend/routing.md) | Routes, screens, guards |
| [frontend/state.md](frontend/state.md) | Client state, server cache, data flow |
| [frontend/ui.md](frontend/ui.md) | UI kit, theme, layout patterns |
| [backend/overview.md](backend/overview.md) | Backend role, stack, entry, boundaries |
| [backend/structure.md](backend/structure.md) | Folders, modules, layers |
| [backend/api.md](backend/api.md) | Endpoints, contracts, errors |
| [backend/data.md](backend/data.md) | Persistence, entities, migrations |
| [backend/security.md](backend/security.md) | Auth, authorization, secrets |
| [backend/google-oauth-setup.md](backend/google-oauth-setup.md) | Local Google Cloud OAuth steps |
| [decisions/](decisions/README.md) | Architecture decision records |

## Update contract

- Edit the matching file in the same change as the code.
- Facts only. Unknown or unused: `None yet.`
- Add a focused file and a row in this table when a topic no longer fits its owner.
