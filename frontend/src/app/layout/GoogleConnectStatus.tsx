import Button from "@mui/material/Button";
import Divider from "@mui/material/Divider";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useState, type MouseEvent } from "react";
import { useNavigate } from "react-router-dom";
import { getGoogleConnection, googleConnectionQueryKey } from "@/shared/api/google";
import { useAuth } from "@/shared/auth/AuthProvider";
import { appPaths } from "@/shared/config/paths";

const StatusTrigger = styled(Button)(({ theme }) => ({
  minHeight: 38,
  minWidth: 0,
  paddingLeft: theme.spacing(1.25),
  paddingRight: theme.spacing(1.5),
  borderRadius: 999,
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  color: theme.palette.text.primary,
  boxShadow: "none",
  "&:hover": {
    backgroundColor: theme.palette.action.hover,
    borderColor: theme.palette.primary.light,
    boxShadow: "none",
  },
}));

const StatusDot = styled("span", {
  shouldForwardProp: (prop) => prop !== "tone",
})<{ tone: "live" | "idle" | "wait" }>(({ theme, tone }) => ({
  width: 9,
  height: 9,
  flexShrink: 0,
  borderRadius: "50%",
  backgroundColor:
    tone === "live" ? "#2F7A55" : tone === "wait" ? theme.palette.secondary.main : theme.palette.text.disabled,
  boxShadow:
    tone === "live"
      ? "0 0 0 4px rgba(47, 122, 85, 0.16)"
      : tone === "wait"
        ? "0 0 0 4px rgba(176, 141, 87, 0.2)"
        : "none",
}));

const StatusCopy = styled("span")({
  display: "flex",
  flexDirection: "column",
  alignItems: "flex-start",
  minWidth: 0,
  lineHeight: 1.15,
});

const StatusDetail = styled("span")(({ theme }) => ({
  display: "block",
  maxWidth: 168,
  overflow: "hidden",
  textOverflow: "ellipsis",
  whiteSpace: "nowrap",
  fontSize: "0.7rem",
  fontWeight: 500,
  color: theme.palette.text.secondary,
  [theme.breakpoints.down("sm")]: {
    display: "none",
  },
}));

const StatusMenu = styled(Menu)(({ theme }) => ({
  "& .MuiPaper-root": {
    minWidth: 260,
    marginTop: theme.spacing(1),
    border: `1px solid ${theme.palette.divider}`,
    boxShadow: "0 16px 40px rgba(14, 39, 68, 0.12)",
  },
}));

const StatusIdentity = styled("div")(({ theme }) => ({
  padding: theme.spacing(1.5, 2, 1.25),
}));

function formatConnectedAt(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function GoogleConnectStatus() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const connectionQuery = useQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });

  const status = connectionQuery.data;
  const connected = status?.connected === true;
  const configured = status?.configured === true;
  const failed = connectionQuery.isError;
  const tone = connected ? "live" : failed ? "idle" : configured ? "wait" : "idle";
  const detail = failed
    ? "Unavailable"
    : connected
      ? (status.googleEmail ?? "Connected")
      : configured
        ? "Not connected"
        : "Setup needed";
  const headline = failed
    ? "Could not load"
    : connected
      ? "Connected"
      : configured
        ? "Not connected"
        : "Not ready";
  const open = Boolean(anchor);

  function openMenu(event: MouseEvent<HTMLElement>) {
    setAnchor(event.currentTarget);
  }

  function closeMenu() {
    setAnchor(null);
  }

  function go(path: string) {
    closeMenu();
    navigate(path);
  }

  const managePath =
    !configured && auth.user?.role === "Admin"
      ? appPaths.settings
      : !configured
        ? appPaths.help
        : appPaths.jobApplication;
  const manageLabel =
    !configured && auth.user?.role === "Admin"
      ? "Open Settings"
      : !configured
        ? "Open Help"
        : "Open Job Application";

  return (
    <>
      <StatusTrigger
        variant="text"
        aria-label={`Gmail ${detail}`}
        aria-controls={open ? "gmail-status-menu" : undefined}
        aria-haspopup="true"
        aria-expanded={open ? "true" : undefined}
        onClick={openMenu}
      >
        <Stack direction="row" spacing={1.25} sx={{ alignItems: "center", minWidth: 0 }}>
          <StatusDot tone={tone} />
          <StatusCopy>
            <Typography variant="body2" component="span">
              Gmail
            </Typography>
            <StatusDetail>{connectionQuery.isPending ? "Checking" : detail}</StatusDetail>
          </StatusCopy>
        </Stack>
      </StatusTrigger>
      <StatusMenu
        id="gmail-status-menu"
        anchorEl={anchor}
        open={open}
        onClose={closeMenu}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
      >
        <StatusIdentity>
          <Stack spacing={0.25}>
            <Typography variant="subtitle2">Gmail</Typography>
            <Typography variant="caption" color="text.secondary">
              {connectionQuery.isPending ? "Checking connection" : headline}
            </Typography>
            {connected && status.googleEmail ? (
              <Typography variant="body2">{status.googleEmail}</Typography>
            ) : null}
            {connected && status.connectedAt ? (
              <Typography variant="caption" color="text.secondary">
                Since {formatConnectedAt(status.connectedAt)}
              </Typography>
            ) : null}
            {!connected && configured ? (
              <Typography variant="body2" color="text.secondary">
                Connect on Job Application Settings. Sheets and Drive use the same consent.
              </Typography>
            ) : null}
            {!configured && !connectionQuery.isPending && !failed ? (
              <Typography variant="body2" color="text.secondary">
                An admin must save the Google Cloud client before anyone can connect Gmail.
              </Typography>
            ) : null}
            {failed ? (
              <Typography variant="body2" color="text.secondary">
                Gmail status could not be loaded. Confirm the API is running.
              </Typography>
            ) : null}
          </Stack>
        </StatusIdentity>
        <Divider />
        <MenuItem onClick={() => go(managePath)}>{manageLabel}</MenuItem>
      </StatusMenu>
    </>
  );
}
