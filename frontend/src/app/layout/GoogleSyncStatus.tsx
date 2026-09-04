import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useEffect, useState } from "react";
import { useGoogleSync } from "@/app/providers/GoogleSyncProvider";
import { formatUpdatedAgo, googleSyncLane } from "@/shared/api/googleSync";

const SyncTrigger = styled(Button, {
  shouldForwardProp: (prop) => prop !== "lane",
})<{ lane: "busy" | "fresh" | "aging" | "stale" }>(({ theme, lane }) => ({
  minHeight: 38,
  minWidth: 0,
  paddingLeft: theme.spacing(1.25),
  paddingRight: theme.spacing(1.5),
  borderRadius: 999,
  border: "1px solid transparent",
  color: theme.palette.primary.contrastText,
  boxShadow: "none",
  backgroundColor:
    lane === "fresh"
      ? "#1F5C40"
      : lane === "aging"
        ? "#8A5A12"
        : lane === "busy"
          ? theme.palette.primary.main
          : "#7A1F28",
  "&:hover": {
    boxShadow: "none",
    backgroundColor:
      lane === "fresh"
        ? "#184A33"
        : lane === "aging"
          ? "#734B0E"
          : lane === "busy"
            ? theme.palette.primary.dark
            : "#641820",
  },
  "&.Mui-disabled": {
    color: theme.palette.common.white,
    opacity: 1,
  },
}));

const Lamp = styled("span", {
  shouldForwardProp: (prop) => prop !== "lit" && prop !== "toneColor" && prop !== "chasing" && prop !== "delayMs",
})<{ lit: boolean; toneColor: string; chasing: boolean; delayMs: number }>(
  ({ lit, toneColor, chasing, delayMs }) => ({
    width: 7,
    height: 7,
    borderRadius: "50%",
    flexShrink: 0,
    backgroundColor: toneColor,
    opacity: chasing ? undefined : lit ? 1 : 0.28,
    boxShadow: lit && !chasing ? `0 0 0 3px ${toneColor}44, 0 0 10px ${toneColor}` : "none",
    animationDelay: `${delayMs}ms`,
    animation: chasing
      ? "flexisLampChase 0.9s ease-in-out infinite"
      : lit
        ? "flexisLampPulse 1.6s ease-in-out infinite"
        : "none",
    "@keyframes flexisLampPulse": {
      "0%, 100%": { opacity: 1 },
      "50%": { opacity: 0.55 },
    },
    "@keyframes flexisLampChase": {
      "0%, 100%": { opacity: 0.22 },
      "50%": { opacity: 1 },
    },
  }),
);

const SyncLabel = styled("span")(({ theme }) => ({
  color: "inherit",
  [theme.breakpoints.down("sm")]: {
    display: "none",
  },
}));

const SYNC_PROGRESS_RING_SIZE = 30;

const ProgressRingRoot = styled(Box)({
  position: "relative",
  width: SYNC_PROGRESS_RING_SIZE,
  height: SYNC_PROGRESS_RING_SIZE,
  flexShrink: 0,
});

const ProgressRingLayer = styled(CircularProgress)({
  position: "absolute",
  top: 0,
  left: 0,
});

const ProgressRingTrack = styled(ProgressRingLayer)({
  color: "rgba(255, 255, 255, 0.22)",
});

const ProgressRingValue = styled(ProgressRingLayer)(({ theme }) => ({
  color: theme.palette.common.white,
  transition: theme.transitions.create("stroke-dashoffset", {
    duration: theme.transitions.duration.shorter,
    easing: theme.transitions.easing.easeInOut,
  }),
  "& .MuiCircularProgress-circle": {
    strokeLinecap: "round",
  },
}));

const ProgressRingLabel = styled(Typography)(({ theme }) => ({
  position: "absolute",
  top: "50%",
  left: "50%",
  transform: "translate(-50%, -50%)",
  fontSize: 9,
  fontWeight: 700,
  lineHeight: 1,
  color: theme.palette.common.white,
  fontVariantNumeric: "tabular-nums",
  pointerEvents: "none",
}));

type SyncProgressRingProps = {
  value: number;
};

function SyncProgressRing({ value }: SyncProgressRingProps) {
  const clamped = Math.min(100, Math.max(0, Math.round(value)));

  return (
    <ProgressRingRoot aria-hidden>
      <ProgressRingTrack
        variant="determinate"
        value={100}
        size={SYNC_PROGRESS_RING_SIZE}
        thickness={3.5}
      />
      <ProgressRingValue
        variant="determinate"
        value={clamped}
        size={SYNC_PROGRESS_RING_SIZE}
        thickness={3.5}
      />
      <ProgressRingLabel variant="caption">
        {clamped}
      </ProgressRingLabel>
    </ProgressRingRoot>
  );
}

export function GoogleSyncStatus() {
  const sync = useGoogleSync();
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 15_000);
    return () => window.clearInterval(timer);
  }, []);

  const lane = googleSyncLane(sync.lastSyncedAt, now, sync.failed, sync.isSyncing);
  const label = sync.isSyncing
    ? "Updating…"
    : sync.lastSyncedAt
      ? formatUpdatedAgo(sync.lastSyncedAt, now)
      : "Waiting for Google";
  const litIndex = lane === "stale" ? 0 : lane === "aging" ? 1 : 2;

  return (
    <SyncTrigger
      variant="text"
      lane={lane}
      disabled={sync.isSyncing}
      aria-label={
        sync.isSyncing
          ? `Updating Google sheets and configuration. ${sync.syncProgress} percent complete.`
          : `${label}. Refresh Google sheets and configuration.`
      }
      onClick={() => {
        void sync.refresh();
      }}
    >
      <Stack direction="row" spacing={1.25} sx={{ alignItems: "center", minWidth: 0 }}>
        {sync.isSyncing ? (
          <SyncProgressRing value={sync.syncProgress} />
        ) : (
          <Stack direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
            <Lamp
              toneColor="#FF4D4D"
              lit={litIndex === 0}
              chasing={lane === "busy"}
              delayMs={0}
            />
            <Lamp
              toneColor="#FFC107"
              lit={litIndex === 1}
              chasing={lane === "busy"}
              delayMs={lane === "busy" ? 180 : 0}
            />
            <Lamp
              toneColor="#3DDC84"
              lit={litIndex === 2}
              chasing={lane === "busy"}
              delayMs={lane === "busy" ? 360 : 0}
            />
          </Stack>
        )}
        <SyncLabel>
          <Typography variant="body2" component="span" sx={{ color: "inherit" }}>
            {label}
          </Typography>
        </SyncLabel>
      </Stack>
    </SyncTrigger>
  );
}
