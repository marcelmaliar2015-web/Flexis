# React Vite MUI frontend

## Context

The UI must stay typed, fast to build, and ready for many screens.

## Decision

Vite plus React plus TypeScript. Feature folders under `src/features`. MUI for UI. TanStack Query for server state. React Router for URLs.

## Consequences

New screens belong in a feature folder. Shared API code stays in `shared/api`. Do not add another state library until client-only state needs it.
