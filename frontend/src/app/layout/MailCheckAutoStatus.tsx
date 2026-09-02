import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled, keyframes } from "@mui/material/styles";
import { useEffect, useState, type MouseEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useMailCheckAuto } from "@/app/providers/MailCheckProvider";
import { formatUpdatedAgo } from "@/shared/api/googleSync";
import { appPaths } from "@/shared/config/paths";

const shimmer = keyframes`
  0% { background-position: 200% center; }
  100% { background-position: -200% center; }
`;

const ripple = keyframes`
  0% { transform: scale(0.45); opacity: 0.85; }
  100% { transform: scale(1.35); opacity: 0; }
`;

const AutoCheckTrigger = styled(Button, {
  shouldForwardProp: (prop) => prop !== "running",
})<{ running: boolean }>(({ theme, running }) => ({
  minHeight: 38,
  minWidth: 0,
  paddingLeft: theme.spacing(1.25),
  paddingRight: theme.spacing(1.5),
  borderRadius: 999,
  color: theme.palette.common.white,
  border: running ? "1px solid rgba(255, 255, 255, 0.42)" : "1px solid transparent",
  boxShadow: running
    ? "0 0 0 1px rgba(91, 79, 207, 0.28), 0 10px 28px rgba(79, 70, 229, 0.38)"
    : "0 4px 16px rgba(30, 77, 107, 0.22)",
  background: running
    ? `linear-gradient(120deg, ${theme.palette.primary.main} 0%, #4F46E5 42%, #7C3AED 72%, #4F46E5 100%)`
    : "linear-gradient(135deg, #1E4D6B 0%, #2A5F86 55%, #3B6F99 100%)",
  backgroundSize: running ? "220% auto" : "100% auto",
  animation: running ? `${shimmer} 2.4s linear infinite` : "none",
  "&:hover": {
    boxShadow: running
      ? "0 0 0 1px rgba(91, 79, 207, 0.36), 0 12px 32px rgba(79, 70, 229, 0.44)"
      : "0 6px 20px rgba(30, 77, 107, 0.3)",
    background: running
      ? `linear-gradient(120deg, ${theme.palette.primary.dark} 0%, #4338CA 42%, #6D28D9 72%, #4338CA 100%)`
      : "linear-gradient(135deg, #184058 0%, #245474 55%, #356484 100%)",
  },
}));

const AutoCheckLabel = styled("span")(({ theme }) => ({
  color: "inherit",
  [theme.breakpoints.down("md")]: {
    display: "none",
  },
}));

const ScanBeaconRoot = styled(Box)({
  position: "relative",
  width: 22,
  height: 22,
  flexShrink: 0,
});

const ScanBeaconCore = styled("span")(({ theme }) => ({
  position: "absolute",
  top: "50%",
  left: "50%",
  width: 8,
  height: 8,
  borderRadius: "50%",
  transform: "translate(-50%, -50%)",
  backgroundColor: theme.palette.common.white,
  boxShadow: "0 0 10px rgba(255, 255, 255, 0.85)",
}));

const ScanBeaconRing = styled("span")({
  position: "absolute",
  inset: 0,
  borderRadius: "50%",
  border: "2px solid rgba(255, 255, 255, 0.72)",
  animation: `${ripple} 1.9s ease-out infinite`,
  "&:nth-of-type(2)": {
    animationDelay: "0.65s",
  },
});

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

function ScanBeacon() {
  return (
    <ScanBeaconRoot aria-hidden>
      <ScanBeaconRing />
      <ScanBeaconRing />
      <ScanBeaconCore />
    </ScanBeaconRoot>
  );
}

export function MailCheckAutoStatus() {
  const navigate = useNavigate();
  const mailCheck = useMailCheckAuto();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 15_000);
    return () => window.clearInterval(timer);
  }, []);

  if (!mailCheck.isLive) {
    return null;
  }

  const running = mailCheck.isRunning;
  const label = running ? "Classifying mail…" : "Auto-check live";
  const lastRunLabel = mailCheck.lastRunAt
    ? formatUpdatedAgo(mailCheck.lastRunAt, now)
    : "Waiting for first run";
  const open = Boolean(anchor);

  function openMenu(event: MouseEvent<HTMLElement>) {
    setAnchor(event.currentTarget);
  }

  function closeMenu() {
    setAnchor(null);
  }

  function goMailCheck() {
    closeMenu();
    navigate(appPaths.mailCheck);
  }

  return (
    <>
      <AutoCheckTrigger
        variant="text"
        running={running}
        aria-label={
          running
            ? "Mail Check auto-check is classifying mail."
            : `Mail Check auto-check is live. Last run ${lastRunLabel}.`
        }
        aria-controls={open ? "mail-check-auto-menu" : undefined}
        aria-haspopup="true"
        aria-expanded={open ? "true" : undefined}
        onClick={openMenu}
      >
        <Stack direction="row" spacing={1.25} sx={{ alignItems: "center", minWidth: 0 }}>
          {running ? (
            <CircularProgress size={20} thickness={4.5} sx={{ color: "common.white" }} />
          ) : (
            <ScanBeacon />
          )}
          <AutoCheckLabel>
            <Typography variant="body2" component="span" sx={{ color: "inherit", fontWeight: 600 }}>
              {label}
            </Typography>
          </AutoCheckLabel>
        </Stack>
      </AutoCheckTrigger>
      <StatusMenu
        id="mail-check-auto-menu"
        anchorEl={anchor}
        open={open}
        onClose={closeMenu}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
      >
        <StatusIdentity>
          <Stack spacing={1}>
            <Stack spacing={0.25}>
              <Typography variant="subtitle2">Mail Check auto-check</Typography>
              <Typography variant="caption" color="text.secondary">
                {running
                  ? "Flexis is classifying new mail on this page."
                  : `Watching for new mail every ${mailCheck.intervalSeconds} seconds while Mail Check stays open.`}
              </Typography>
            </Stack>
            <Stack spacing={0.25}>
              <Typography variant="body2">Status</Typography>
              <Typography variant="caption" color="text.secondary">
                {running ? "Classifying now" : "Live and waiting"}
              </Typography>
            </Stack>
            <Stack spacing={0.25}>
              <Typography variant="body2">Last classified</Typography>
              <Typography variant="caption" color="text.secondary">
                {lastRunLabel}
              </Typography>
            </Stack>
          </Stack>
        </StatusIdentity>
        <Divider />
        <MenuItem onClick={goMailCheck}>Open Mail Check</MenuItem>
      </StatusMenu>
    </>
  );
}
