import Avatar from "@mui/material/Avatar";
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useState, type MouseEvent } from "react";
import { Link as RouterLink } from "react-router-dom";
import { useAuth } from "@/shared/auth/AuthProvider";
import { appPaths } from "@/shared/config/paths";
import type { UserDto } from "@/shared/types/user";

const AccountButton = styled(IconButton)(({ theme }) => ({
  padding: 3,
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  "&:hover": {
    backgroundColor: theme.palette.action.hover,
    borderColor: theme.palette.primary.light,
  },
}));

const AccountAvatar = styled(Avatar)(({ theme }) => ({
  width: 32,
  height: 32,
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  fontSize: "0.875rem",
  fontWeight: 600,
}));

const AccountMenu = styled(Menu)(({ theme }) => ({
  "& .MuiPaper-root": {
    minWidth: 240,
    marginTop: theme.spacing(1),
    border: `1px solid ${theme.palette.divider}`,
    boxShadow: "0 16px 40px rgba(14, 39, 68, 0.12)",
  },
}));

const Identity = styled("div")(({ theme }) => ({
  padding: theme.spacing(1.5, 2, 1.25),
}));

function initials(user: UserDto): string {
  const parts = user.displayName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return user.email.slice(0, 1).toUpperCase();
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 1).toUpperCase();
  }
  return `${parts[0].slice(0, 1)}${parts[1].slice(0, 1)}`.toUpperCase();
}

export function UserMenu() {
  const auth = useAuth();
  const user = auth.user;
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  if (!user) {
    return null;
  }

  function openMenu(event: MouseEvent<HTMLElement>) {
    setAnchor(event.currentTarget);
  }

  function closeMenu() {
    setAnchor(null);
  }

  const open = Boolean(anchor);

  return (
    <>
      <AccountButton
        aria-label="Account menu"
        aria-controls={open ? "account-menu" : undefined}
        aria-haspopup="true"
        aria-expanded={open ? "true" : undefined}
        onClick={openMenu}
      >
        <AccountAvatar>{initials(user)}</AccountAvatar>
      </AccountButton>
      <AccountMenu
        id="account-menu"
        anchorEl={anchor}
        open={open}
        onClose={closeMenu}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
      >
        <Identity>
          <Stack spacing={0.25}>
            <Typography variant="subtitle2">{user.displayName}</Typography>
            <Typography variant="caption" color="text.secondary">
              {user.email}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {user.role}
            </Typography>
          </Stack>
        </Identity>
        <Divider />
        <MenuItem component={RouterLink} to={appPaths.settings} onClick={closeMenu}>
          Settings
        </MenuItem>
        <MenuItem component={RouterLink} to={appPaths.help} onClick={closeMenu}>
          Help
        </MenuItem>
        <Divider />
        <MenuItem
          onClick={() => {
            closeMenu();
            auth.signOut();
          }}
        >
          Sign out
        </MenuItem>
      </AccountMenu>
    </>
  );
}
