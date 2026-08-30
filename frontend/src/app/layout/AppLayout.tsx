import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { Link as RouterLink, Outlet } from "react-router-dom";
import { useAuth } from "@/shared/auth/AuthProvider";
import { appPaths } from "@/shared/config/paths";

const ShellRoot = styled(Box)({
  minHeight: "100dvh",
  display: "flex",
  flexDirection: "column",
});

const ShellBar = styled(AppBar)(({ theme }) => ({
  backgroundColor: theme.palette.background.paper,
  color: theme.palette.text.primary,
  borderBottom: `1px solid ${theme.palette.divider}`,
  backgroundImage: "none",
}));

const BrandMark = styled("span")(({ theme }) => ({
  width: 18,
  height: 18,
  borderRadius: 5,
  display: "inline-block",
  flexShrink: 0,
  background: `linear-gradient(145deg, ${theme.palette.primary.main} 35%, ${theme.palette.secondary.main} 100%)`,
}));

const ShellMain = styled("main")({
  flex: 1,
  display: "flex",
  flexDirection: "column",
});

export function AppLayout() {
  const auth = useAuth();

  return (
    <ShellRoot>
      <ShellBar position="sticky">
        <Toolbar>
          <Box
            component={RouterLink}
            to={auth.user ? appPaths.dashboard : appPaths.home}
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 1.25,
              flexGrow: 1,
              minWidth: 0,
              textDecoration: "none",
              color: "inherit",
            }}
          >
            <BrandMark />
            <Typography variant="h6" component="span">
              Flexis
            </Typography>
          </Box>
          <Stack direction="row" spacing={0.5} sx={{ flexShrink: 0, alignItems: "center" }}>
            <Box sx={{ display: { xs: "none", sm: "block" } }}>
              <Button color="inherit" component={RouterLink} to={appPaths.home} variant="text">
                Home
              </Button>
            </Box>
            {auth.user ? (
              <Button color="inherit" component={RouterLink} to={appPaths.health} variant="text">
                Health
              </Button>
            ) : null}
            {auth.user?.role === "Admin" ? (
              <Button color="inherit" component={RouterLink} to={appPaths.users} variant="text">
                Users
              </Button>
            ) : null}
            {auth.user ? (
              <>
                <Box sx={{ display: { xs: "none", md: "block" }, px: 1 }}>
                  <Typography variant="body2" color="text.secondary">
                    {auth.user.displayName}
                  </Typography>
                </Box>
                <Button color="inherit" variant="text" onClick={auth.signOut}>
                  Sign out
                </Button>
              </>
            ) : (
              <Button color="inherit" component={RouterLink} to={appPaths.signIn} variant="text">
                Sign in
              </Button>
            )}
          </Stack>
        </Toolbar>
      </ShellBar>
      <ShellMain>
        <Outlet />
      </ShellMain>
    </ShellRoot>
  );
}
