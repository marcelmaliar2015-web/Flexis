import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import LinearProgress from "@mui/material/LinearProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled, keyframes } from "@mui/material/styles";
import type { PipelineBulkSession } from "@/features/jobApplication/usePipelineBulkRun";

const shimmer = keyframes`
  0% { background-position: 200% center; }
  100% { background-position: -200% center; }
`;

const ProgressRoot = styled(Box, {
  shouldForwardProp: (prop) => prop !== "running",
})<{ running: boolean }>(({ theme, running }) => ({
  border: `1px solid ${running ? theme.palette.primary.main : theme.palette.success.main}`,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2),
  background: running
    ? `linear-gradient(120deg, ${theme.palette.action.hover} 0%, rgba(30, 77, 107, 0.08) 45%, ${theme.palette.action.hover} 100%)`
    : theme.palette.action.hover,
  backgroundSize: running ? "220% auto" : "100% auto",
  animation: running ? `${shimmer} 2.8s linear infinite` : "none",
}));

const ProgressTrack = styled(LinearProgress)(({ theme }) => ({
  height: 8,
  borderRadius: 999,
  backgroundColor: theme.palette.action.selected,
  "& .MuiLinearProgress-bar": {
    borderRadius: 999,
  },
}));

const CountBadge = styled("span")(({ theme }) => ({
  display: "inline-flex",
  alignItems: "center",
  justifyContent: "center",
  minWidth: 52,
  padding: theme.spacing(0.25, 1),
  borderRadius: 999,
  fontSize: "0.75rem",
  fontWeight: 700,
  fontVariantNumeric: "tabular-nums",
  color: theme.palette.primary.contrastText,
  backgroundColor: theme.palette.primary.main,
}));

function actionTitle(action: PipelineBulkSession["action"]): string {
  if (action === "update") {
    return "Update All";
  }
  if (action === "forward") {
    return "Forward All";
  }
  return "Delete All";
}

type PipelineBulkProgressProps = {
  session: PipelineBulkSession;
};

export function PipelineBulkProgress({ session }: PipelineBulkProgressProps) {
  const running = session.phase === "running";
  const inProgress = running ? 1 : 0;
  const percent =
    session.total > 0
      ? Math.min(100, Math.round(((session.current + inProgress) / session.total) * 100))
      : 0;
  const countLabel =
    session.action === "delete"
      ? running
        ? "Working"
        : "Done"
      : session.total > 0
        ? `${Math.min(session.current + inProgress, session.total)}/${session.total}`
        : "0/0";

  return (
    <ProgressRoot running={running} role="status" aria-live="polite">
      <Stack spacing={1.25}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
          {running ? <CircularProgress size={22} thickness={4.5} /> : null}
          <Stack spacing={0.25} sx={{ flex: 1, minWidth: 0 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center", flexWrap: "wrap" }}>
              <Typography variant="subtitle2">{actionTitle(session.action)}</Typography>
              <CountBadge>{countLabel}</CountBadge>
            </Stack>
            <Typography variant="body2">{session.message}</Typography>
            {session.detail ? (
              <Typography variant="caption" color="text.secondary" noWrap title={session.detail}>
                {session.detail}
              </Typography>
            ) : null}
          </Stack>
        </Stack>
        <ProgressTrack
          variant={session.action === "delete" && running ? "indeterminate" : "determinate"}
          value={session.action === "delete" && running ? undefined : running ? percent : 100}
          color={running ? "primary" : "success"}
        />
      </Stack>
    </ProgressRoot>
  );
}
