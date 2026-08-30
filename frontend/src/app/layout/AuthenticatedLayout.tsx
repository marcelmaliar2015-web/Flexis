import Box from "@mui/material/Box";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemText from "@mui/material/ListItemText";
import { styled } from "@mui/material/styles";
import { Link as RouterLink, Outlet, useLocation } from "react-router-dom";
import { appPaths } from "@/shared/config/paths";

const AuthenticatedRoot = styled(Box)({
  flex: 1,
  display: "flex",
  minHeight: 0,
});

const SidePanel = styled("nav")(({ theme }) => ({
  width: 240,
  flexShrink: 0,
  padding: theme.spacing(1),
  borderRight: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
}));

const appNavItems = [
  { label: "Dashboard", path: appPaths.dashboard },
  { label: "Job Application", path: appPaths.jobApplication },
  { label: "Settings", path: appPaths.settings },
  { label: "Help", path: appPaths.help },
] as const;

function isAppNavActive(pathname: string, path: string) {
  return pathname === path || pathname.startsWith(`${path}/`);
}

export function AuthenticatedLayout() {
  const location = useLocation();

  return (
    <AuthenticatedRoot>
      <SidePanel>
        <List>
          {appNavItems.map((item) => (
            <ListItem key={item.path} disablePadding>
              <ListItemButton
                component={RouterLink}
                to={item.path}
                selected={isAppNavActive(location.pathname, item.path)}
              >
                <ListItemText primary={item.label} />
              </ListItemButton>
            </ListItem>
          ))}
        </List>
      </SidePanel>
      <Box component="section" sx={{ flex: 1, minWidth: 0, overflow: "auto" }}>
        <Outlet />
      </Box>
    </AuthenticatedRoot>
  );
}
