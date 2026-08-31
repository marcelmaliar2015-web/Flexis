import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "@/shared/auth/AuthProvider";
import { appPaths } from "@/shared/config/paths";

function AuthPending() {
  return (
    <Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
      <CircularProgress />
    </Box>
  );
}

export function RequireAuth() {
  const auth = useAuth();
  const location = useLocation();

  if (!auth.isReady) {
    return <AuthPending />;
  }

  if (!auth.user) {
    return <Navigate to={appPaths.signIn} replace state={{ from: location }} />;
  }

  return <Outlet />;
}

export function RequireGuest() {
  const auth = useAuth();
  const location = useLocation();

  if (!auth.isReady) {
    return <AuthPending />;
  }

  if (auth.user) {
    return <Navigate to={signedInLandingPath(location.state)} replace />;
  }

  return <Outlet />;
}

function signedInLandingPath(state: unknown): string {
  if (!state || typeof state !== "object" || !("from" in state)) {
    return appPaths.dashboard;
  }

  const from = state.from;
  if (!from || typeof from !== "object" || !("pathname" in from)) {
    return appPaths.dashboard;
  }

  const pathname = from.pathname;
  if (typeof pathname !== "string" || !pathname.startsWith("/") || pathname.startsWith("//")) {
    return appPaths.dashboard;
  }

  if (pathname === appPaths.home || pathname === appPaths.signIn) {
    return appPaths.dashboard;
  }

  return pathname;
}
