import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "@/shared/auth/AuthProvider";
import { appPaths } from "@/shared/config/paths";

export function RequireAuth() {
  const auth = useAuth();
  const location = useLocation();

  if (!auth.isReady) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!auth.user) {
    return <Navigate to={appPaths.signIn} replace state={{ from: location }} />;
  }

  return <Outlet />;
}

export function RequireAdmin() {
  const auth = useAuth();

  if (auth.user?.role !== "Admin") {
    return <Navigate to={appPaths.dashboard} replace />;
  }

  return <Outlet />;
}
