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
import { getMailCheckMailbox, mailCheckMailboxQueryKey } from "@/shared/api/mailCheck";
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

const ProviderMark = styled("span")(({ theme }) => ({
  display: "inline-flex",
  alignItems: "center",
  gap: theme.spacing(0.75),
  minWidth: 0,
}));

const StatusMenu = styled(Menu)(({ theme }) => ({
  "& .MuiPaper-root": {
    minWidth: 280,
    marginTop: theme.spacing(1),
    border: `1px solid ${theme.palette.divider}`,
    boxShadow: "0 16px 40px rgba(14, 39, 68, 0.12)",
  },
}));

const StatusIdentity = styled("div")(({ theme }) => ({
  padding: theme.spacing(1.5, 2, 1.25),
}));

function clientTone(ready: boolean | undefined, failed: boolean, pending: boolean): "live" | "idle" | "wait" {
  if (failed) {
    return "idle";
  }
  if (pending || ready === undefined) {
    return "wait";
  }
  return ready ? "live" : "idle";
}

function clientLabel(ready: boolean | undefined, failed: boolean, pending: boolean): string {
  if (failed) {
    return "Unavailable";
  }
  if (pending || ready === undefined) {
    return "Checking";
  }
  return ready ? "Ready" : "Setup needed";
}

export function ClientIntegrationsStatus() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const googleQuery = useQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });
  const microsoftQuery = useQuery({
    queryKey: mailCheckMailboxQueryKey,
    queryFn: getMailCheckMailbox,
  });

  const googleReady = googleQuery.data?.configured;
  const microsoftReady = microsoftQuery.data?.outlookAvailable;
  const googleFailed = googleQuery.isError;
  const microsoftFailed = microsoftQuery.isError;
  const googlePending = googleQuery.isPending;
  const microsoftPending = microsoftQuery.isPending;
  const open = Boolean(anchor);
  const isAdmin = auth.user?.role === "Admin";
  const managePath = isAdmin ? appPaths.settings : appPaths.help;
  const manageLabel = isAdmin ? "Open Settings" : "Open Help";

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

  return (
    <>
      <StatusTrigger
        variant="text"
        aria-label={`Google client ${clientLabel(googleReady, googleFailed, googlePending)}. Microsoft client ${clientLabel(microsoftReady, microsoftFailed, microsoftPending)}.`}
        aria-controls={open ? "client-integrations-menu" : undefined}
        aria-haspopup="true"
        aria-expanded={open ? "true" : undefined}
        onClick={openMenu}
      >
        <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", minWidth: 0 }}>
          <ProviderMark>
            <StatusDot tone={clientTone(googleReady, googleFailed, googlePending)} />
            <Typography variant="body2" component="span">
              Google
            </Typography>
          </ProviderMark>
          <ProviderMark>
            <StatusDot tone={clientTone(microsoftReady, microsoftFailed, microsoftPending)} />
            <Typography variant="body2" component="span">
              Microsoft
            </Typography>
          </ProviderMark>
        </Stack>
      </StatusTrigger>
      <StatusMenu
        id="client-integrations-menu"
        anchorEl={anchor}
        open={open}
        onClose={closeMenu}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
      >
        <StatusIdentity>
          <Stack spacing={1.5}>
            <Stack spacing={0.25}>
              <Typography variant="subtitle2">Client integrations</Typography>
              <Typography variant="caption" color="text.secondary">
                Admin OAuth apps used for Connect Gmail and Connect Outlook. Your mailbox links stay separate.
              </Typography>
            </Stack>
            <Stack spacing={0.5}>
              <Typography variant="body2">Google Cloud client</Typography>
              <Typography variant="caption" color="text.secondary">
                {clientLabel(googleReady, googleFailed, googlePending)}
                {googleReady ? ". Job Application and Mail Check Gmail can connect." : null}
                {!googleReady && !googlePending && !googleFailed
                  ? ". An admin must save Client ID and secret on Settings."
                  : null}
                {googleFailed ? ". Status could not be loaded." : null}
              </Typography>
            </Stack>
            <Stack spacing={0.5}>
              <Typography variant="body2">Microsoft client</Typography>
              <Typography variant="caption" color="text.secondary">
                {clientLabel(microsoftReady, microsoftFailed, microsoftPending)}
                {microsoftReady ? ". Mail Check Outlook can connect." : null}
                {!microsoftReady && !microsoftPending && !microsoftFailed
                  ? ". An admin must save Application ID and secret on Settings."
                  : null}
                {microsoftFailed ? ". Status could not be loaded." : null}
              </Typography>
            </Stack>
          </Stack>
        </StatusIdentity>
        <Divider />
        <MenuItem onClick={() => go(managePath)}>{manageLabel}</MenuItem>
      </StatusMenu>
    </>
  );
}
