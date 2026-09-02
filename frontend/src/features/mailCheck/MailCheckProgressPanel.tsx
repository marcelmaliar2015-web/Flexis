import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import LinearProgress from "@mui/material/LinearProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import {
  formatElapsedMs,
  mailCheckRunStages,
  type MailCheckRunStageId,
} from "@/features/mailCheck/mailCheckStages";
import { mailCheckTimingRows } from "@/features/mailCheck/mailCheckUi";
import type { MailCheckCheckSession } from "@/features/mailCheck/mailCheckRunSession";
import type { MailCheckRunProgress, MailCheckRunTiming } from "@/shared/types/mailCheck";

const StageRow = styled(Box, {
  shouldForwardProp: (prop) => prop !== "state",
})<{ state: "pending" | "active" | "done" }>(({ theme, state }) => ({
  border: `1px solid ${state === "active" ? theme.palette.primary.main : theme.palette.divider}`,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(1.25, 1.5),
  backgroundColor:
    state === "active"
      ? theme.palette.action.hover
      : state === "done"
        ? theme.palette.background.paper
        : theme.palette.action.disabledBackground,
  opacity: state === "pending" ? 0.72 : 1,
}));

const LiveStat = styled(Stack)(({ theme }) => ({
  minWidth: 88,
  gap: theme.spacing(0.25),
}));

type MailCheckProgressPanelProps = {
  session: MailCheckCheckSession;
  checking: boolean;
  elapsedMs: number;
  serverWaitMs: number;
  liveProgress: MailCheckRunProgress | null;
  autoCheckLive?: boolean;
  interruptedByRefresh?: boolean;
};

const stageOrder = mailCheckRunStages.map((stage) => stage.id);

function parseLiveStage(stage: string): MailCheckRunStageId | null {
  return stageOrder.find((item) => item === stage) ?? null;
}

function stageState(
  stageId: MailCheckRunStageId,
  timing: MailCheckRunTiming,
  stage: (typeof mailCheckRunStages)[number],
  liveStage: MailCheckRunStageId | null,
  liveActive: boolean,
  finished: boolean,
): "pending" | "active" | "done" {
  const ms = stage.timingKey ? timing[stage.timingKey] : 0;
  if (ms > 0) {
    return "done";
  }

  if (finished) {
    const stageIndex = stageOrder.indexOf(stageId);
    const lastDoneIndex = stageOrder.reduce((last, id, index) => {
      const key = mailCheckRunStages[index]?.timingKey;
      return key && timing[key] > 0 ? index : last;
    }, -1);
    if (stageIndex >= 0 && stageIndex <= Math.max(lastDoneIndex, 0)) {
      return "done";
    }

    return "pending";
  }

  if (!liveActive || !liveStage) {
    return "pending";
  }

  const liveIndex = stageOrder.indexOf(liveStage);
  const stageIndex = stageOrder.indexOf(stageId);
  if (stageIndex < liveIndex) {
    return "done";
  }

  if (stageIndex === liveIndex) {
    return "active";
  }

  return "pending";
}

export function MailCheckProgressPanel({
  session,
  checking,
  elapsedMs,
  serverWaitMs,
  liveProgress,
  autoCheckLive = false,
  interruptedByRefresh = false,
}: MailCheckProgressPanelProps) {
  const timing = session.totals.timing;
  const rows = mailCheckTimingRows(timing);
  const visibleRows = rows.filter((row) => row.ms > 0);
  const maxMs = Math.max(...visibleRows.map((row) => row.ms), 1);
  const lockWaitActive = checking && Boolean(session.waitingForLock || liveProgress?.waitingForLock);
  const serverRoundActive = checking && Boolean(session.serverWaitStartedAt) && !lockWaitActive;
  const liveActive = Boolean(liveProgress?.active && !liveProgress.waitingForLock);
  const liveStage = liveProgress ? parseLiveStage(liveProgress.stage) : null;
  const liveMessage = liveProgress?.message ?? session.stageMessage ?? session.message;
  const blockingRun =
    liveProgress?.activeRunKind === "auto"
      ? "Auto-check"
      : liveProgress?.activeRunKind === "manual"
        ? "Manual check"
        : null;
  const sessionProcessed = session.totals.processed;
  const roundProcessed = liveActive ? liveProgress?.processed ?? 0 : 0;
  const roundScanned = liveActive ? liveProgress?.scanned ?? 0 : 0;
  const roundAlreadySeen = liveActive ? liveProgress?.alreadySeen ?? 0 : 0;
  const finished = !checking && (session.phase === "done" || session.phase === "cancelled");

  return (
    <Stack spacing={1.5}>
      {interruptedByRefresh ? (
        <Alert severity="warning">
          This page was refreshed during a manual check. The banner reset, but auto-check may still
          be running in the AppBar when enabled. Open the Check tab and run Check again to continue
          manually.
        </Alert>
      ) : null}

      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={1}
        sx={{ alignItems: { sm: "center" }, justifyContent: "space-between" }}
      >
        <Stack spacing={0.25}>
          <Typography variant="subtitle1">
            {checking ? "Manual check in progress" : "Last manual check"}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {liveMessage ?? "Waiting to start…"}
          </Typography>
        </Stack>
        <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
          {checking ? <Chip size="small" color="primary" label={`Elapsed ${formatElapsedMs(elapsedMs)}`} /> : null}
          {serverRoundActive ? (
            <Chip size="small" color="primary" variant="outlined" label={`Server round ${formatElapsedMs(serverWaitMs)}`} />
          ) : null}
          {autoCheckLive ? <Chip size="small" color="secondary" variant="outlined" label="Auto-check live" /> : null}
        </Stack>
      </Stack>

      {checking ? (
        <Box
          sx={{
            border: 1,
            borderColor: liveActive ? "primary.main" : "divider",
            borderRadius: 1,
            p: 1.5,
            bgcolor: liveActive ? "action.hover" : "background.paper",
          }}
        >
          <Stack spacing={1}>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
              <Typography variant="subtitle2">Current server round</Typography>
              {liveStage ? <Chip size="small" color="primary" label={mailCheckRunStages.find((s) => s.id === liveStage)?.label ?? liveStage} /> : null}
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {liveActive
                ? liveMessage
                : lockWaitActive
                  ? liveMessage ??
                    (blockingRun
                      ? `Waiting for server lock — ${blockingRun} is finishing its current round`
                      : "Waiting for server lock — another Mail Check run is active")
                  : serverRoundActive
                    ? "Connecting to server…"
                    : "Starting manual check…"}
            </Typography>
            <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap", rowGap: 1.5 }}>
              <LiveStat>
                <Typography variant="caption" color="text.secondary">
                  Classified this round
                </Typography>
                <Typography variant="subtitle1" color={roundProcessed > 0 ? "primary" : "text.primary"}>
                  {roundProcessed}
                </Typography>
              </LiveStat>
              <LiveStat>
                <Typography variant="caption" color="text.secondary">
                  Candidates scanned
                </Typography>
                <Typography variant="subtitle1" color={roundScanned > 0 ? "primary" : "text.primary"}>
                  {roundScanned}
                </Typography>
              </LiveStat>
              <LiveStat>
                <Typography variant="caption" color="text.secondary">
                  Already labeled
                </Typography>
                <Typography variant="subtitle1">{roundAlreadySeen}</Typography>
              </LiveStat>
              <LiveStat>
                <Typography variant="caption" color="text.secondary">
                  Session total classified
                </Typography>
                <Typography variant="subtitle1">{sessionProcessed + roundProcessed}</Typography>
              </LiveStat>
            </Stack>
            {liveActive && liveStage === "scan" && roundScanned > 0 && roundProcessed === 0 ? (
              <Typography variant="caption" color="text.secondary">
                Scanning can take several minutes on a large inbox. Flexis is skipping mail that
                already has a Flexis label or was processed before.
              </Typography>
            ) : null}
            {serverRoundActive ? <LinearProgress /> : null}
          </Stack>
        </Box>
      ) : null}

      {lockWaitActive ? (
        <Alert severity="warning" icon={false}>
          <Stack spacing={0.5}>
            <Typography variant="body2">
              {blockingRun
                ? `${blockingRun} still holds the server lock. Manual Check requests cancel, then waits up to 20 seconds per try.`
                : "Another Mail Check run still holds the server lock. Waiting up to 20 seconds per try."}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Need action and Inbox do not use this lock. Only classify runs share it.
            </Typography>
            <LinearProgress />
          </Stack>
        </Alert>
      ) : null}

      <Stack spacing={1}>
        <Typography variant="subtitle2">Stages</Typography>
        {mailCheckRunStages.map((stage) => {
          const state = stageState(stage.id, timing, stage, liveStage, liveActive, finished);
          const ms = stage.timingKey ? timing[stage.timingKey] : 0;
          return (
            <StageRow key={stage.id} state={state}>
              <Stack spacing={0.25}>
                <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                  <Typography variant="body2">{stage.label}</Typography>
                  {state === "active" ? <Chip size="small" color="primary" label="Now" /> : null}
                  {ms > 0 ? (
                    <Typography variant="caption" color="text.secondary">
                      {formatElapsedMs(ms)}
                    </Typography>
                  ) : state === "done" ? (
                    <Typography variant="caption" color="text.secondary">
                      done
                    </Typography>
                  ) : null}
                </Stack>
                <Typography variant="caption" color="text.secondary">
                  {stage.description}
                </Typography>
              </Stack>
            </StageRow>
          );
        })}
      </Stack>

      <Stack spacing={1}>
        <Stack spacing={0.25}>
          <Typography variant="subtitle2">Time breakdown this session</Typography>
          <Typography variant="caption" color="text.secondary">
            Pipeline order. Values are summed across server rounds. Wait for lock is only the time
            blocked before a round starts.
          </Typography>
        </Stack>
        {visibleRows.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {checking
              ? "Step times appear after each server round finishes."
              : "Run Check to collect timing."}
          </Typography>
        ) : (
          visibleRows.map((row) => (
            <Stack key={row.label} spacing={0.5}>
              <Stack direction="row" spacing={1} sx={{ justifyContent: "space-between" }}>
                <Typography variant="body2">{row.label}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {formatElapsedMs(row.ms)}
                </Typography>
              </Stack>
              <LinearProgress
                variant="determinate"
                value={Math.max(8, (row.ms / maxMs) * 100)}
                sx={{ height: 8, borderRadius: 1 }}
              />
              <Typography variant="caption" color="text.secondary">
                {row.hint}
              </Typography>
            </Stack>
          ))
        )}
      </Stack>
    </Stack>
  );
}
