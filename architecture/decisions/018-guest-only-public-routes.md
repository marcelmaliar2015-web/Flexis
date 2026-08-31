# Guest-only landing and sign-in

## Context

Signed-in people could still open `/` and `/sign-in`. The landing is a visitor page. The sign-in form is not an authenticated screen. Architecture already said signed-in visits to `/sign-in` go to Dashboard, but `/` stayed public and the sign-in page did not wait for session restore.

## Decision

`RequireGuest` wraps `/` and `/sign-in`. While the session is loading, both show the same pending spinner as `RequireAuth`. If a user is signed in, they are sent to Dashboard, or to the protected path stored in `state.from` after a login that started from a blocked product URL. `/` and `/sign-in` are not valid return paths.

## Consequences

Do not render Home or Sign in for a signed-in session. Product chrome that points home for visitors points to Dashboard when signed in. New public marketing routes belong under `RequireGuest`.

## Related

- [005-signed-in-left-nav.md](005-signed-in-left-nav.md)
