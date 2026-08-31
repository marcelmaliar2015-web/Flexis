import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { Link as RouterLink, Outlet } from "react-router-dom";
import { GoogleConnectStatus } from "@/app/layout/GoogleConnectStatus";
import { GoogleSyncStatus } from "@/app/layout/GoogleSyncStatus";
import { UserMenu } from "@/app/layout/UserMenu";
import { useAuth } from "@/shared/auth/AuthProvider";
import { appPaths } from "@/shared/config/paths";

const ShellRoot = styled(Box)({
  height: "100dvh",
  display: "flex",
  flexDirection: "column",
  overflow: "hidden",
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
  minHeight: 0,
  overflow: "auto",
});

const HeaderRail = styled("span")(({ theme }) => ({
  width: 1,
  height: 22,
  flexShrink: 0,
  backgroundColor: theme.palette.divider,
}));

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
          {auth.user ? (
            <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", flexShrink: 0 }}>
              <GoogleSyncStatus />
              <GoogleConnectStatus />
              <HeaderRail />
              <UserMenu />
            </Stack>
          ) : (
            <Button color="inherit" component={RouterLink} to={appPaths.signIn} variant="text">
              Sign in
            </Button>
          )}
        </Toolbar>
      </ShellBar>
      <ShellMain>
        <Outlet />
      </ShellMain>
    </ShellRoot>
  );
}
