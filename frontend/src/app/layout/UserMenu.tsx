import Avatar from "@mui/material/Avatar";
import Chip from "@mui/material/Chip";
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
import { userInitials } from "@/shared/auth/userInitials";
import { appPaths } from "@/shared/config/paths";

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

const HeaderAvatar = styled(Avatar)(({ theme }) => ({
  width: 40,
  height: 40,
  flexShrink: 0,
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  fontSize: "0.95rem",
  fontWeight: 600,
}));

const AccountMenu = styled(Menu)(({ theme }) => ({
  "& .MuiPaper-root": {
    width: 280,
    marginTop: theme.spacing(1),
    border: `1px solid ${theme.palette.divider}`,
    boxShadow: "0 16px 40px rgba(14, 39, 68, 0.12)",
  },
}));

const Identity = styled("div")(({ theme }) => ({
  display: "flex",
  alignItems: "center",
  gap: theme.spacing(1.5),
  padding: theme.spacing(1.75, 2, 1.5),
}));

const IdentityCopy = styled("div")({
  minWidth: 0,
  display: "flex",
  flexDirection: "column",
  gap: 2,
});

const EmailLine = styled(Typography)({
  overflow: "hidden",
  textOverflow: "ellipsis",
  whiteSpace: "nowrap",
});

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
        <AccountAvatar>{userInitials(user)}</AccountAvatar>
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
          <HeaderAvatar>{userInitials(user)}</HeaderAvatar>
          <IdentityCopy>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center", minWidth: 0 }}>
              <Typography variant="subtitle2" noWrap>
                {user.displayName}
              </Typography>
              <Chip size="small" label={user.role} />
            </Stack>
            <EmailLine variant="caption" color="text.secondary">
              {user.email}
            </EmailLine>
          </IdentityCopy>
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
