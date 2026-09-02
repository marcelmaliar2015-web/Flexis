import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import LinearProgress from "@mui/material/LinearProgress";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRef, useState } from "react";
import { EmptyState, Panel } from "@/features/mailCheck/mailCheckLayout";
import {
  applySessionRound,
  createSession,
  type MailCheckCheckSession,
  type MailboxCheckStats,
} from "@/features/mailCheck/mailCheckRunSession";
import { actionLabel, errorMessage, formatMailboxScanStatus, providerLabel } from "@/features/mailCheck/mailCheckUi";
import { isAbortError } from "@/shared/api/client";
import {
  getMailCheckSettings,
  mailCheckInboxRootQueryKey,
  mailCheckLastRunQueryKey,
  mailCheckNeedActionQueryKey,
  mailCheckSettingsQueryKey,
  runMailCheck,
} from "@/shared/api/mailCheck";
import type { MailCheckMailboxItem } from "@/shared/types/mailCheck";

const maxRounds = 500;
const busyWaitMs = 1500;
const maxBusyRetries = 20;

const MailboxCard = styled(Box, {
  shouldForwardProp: (prop) => prop !== "active",
})<{ active: boolean }>(({ theme, active }) => ({
  border: `1px solid ${active ? theme.palette.primary.main : theme.palette.divider}`,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2),
  backgroundColor: active ? theme.palette.action.hover : theme.palette.background.paper,
  transition: theme.transitions.create(["border-color", "background-color"], {
    duration: theme.transitions.duration.shortest,
  }),
}));

const StatBlock = styled(Stack)(({ theme }) => ({
  minWidth: 72,
  gap: theme.spacing(0.25),
}));

function wait(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    const id = window.setTimeout(resolve, ms);
    if (!signal) {
      return;
    }

    if (signal.aborted) {
      window.clearTimeout(id);
      reject(new DOMException("Aborted", "AbortError"));
      return;
    }

    signal.addEventListener(
      "abort",
      () => {
        window.clearTimeout(id);
        reject(new DOMException("Aborted", "AbortError"));
      },
      { once: true },
    );
  });
}

async function runUntilCaughtUp(
  mailboxes: MailCheckMailboxItem[],
  mailboxId: string | null,
  onProgress: (session: MailCheckCheckSession) => void,
  signal?: AbortSignal,
): Promise<MailCheckCheckSession> {
  let session = createSession(mailboxes);
  let rounds = 0;
  let busyRetries = 0;

  onProgress({
    ...session,
    phase: "scanning",
    message: mailboxId ? "Starting mailbox check…" : "Starting check on all mailboxes…",
  });

  try {
    while (rounds < maxRounds) {
      if (signal?.aborted) {
        break;
      }

      const next = await runMailCheck({
        force: true,
        mailboxId,
        resetCursor: rounds === 0,
        signal,
      });

      if (next.busy) {
        busyRetries += 1;
        if (busyRetries > maxBusyRetries) {
          session = applySessionRound(session, { ...next, hasMore: true }, false);
          onProgress(session);
          return session;
        }

        session = applySessionRound(session, next, false);
        onProgress(session);
        await wait(busyWaitMs, signal);
        continue;
      }

      busyRetries = 0;
      if (next.processed > 0) {
        rounds += 1;
      }

      session = applySessionRound(session, next, false);
      onProgress(session);

      if (!next.hasMore) {
        break;
      }
    }
  } catch (error) {
    if (!isAbortError(error)) {
      throw error;
    }
  }

  if (signal?.aborted) {
    session = {
      ...session,
      phase: "cancelled",
      message:
        session.totals.processed > 0
          ? `Stopped after ${session.totals.processed} message${session.totals.processed === 1 ? "" : "s"}`
          : "Stopped before any messages were processed",
    };
  } else if (session.phase !== "done") {
    session = { ...session, phase: "done" };
  }

  onProgress(session);
  return session;
}

export function MailCheckCheckTab() {
  const queryClient = useQueryClient();
  const settingsQuery = useQuery({
    queryKey: mailCheckSettingsQueryKey,
    queryFn: getMailCheckSettings,
  });
  const sessionQuery = useQuery({
    queryKey: mailCheckLastRunQueryKey,
    queryFn: async () => null as MailCheckCheckSession | null,
    enabled: false,
    staleTime: Infinity,
  });
  const [runError, setRunError] = useState<string | null>(null);
  const [activeMailboxId, setActiveMailboxId] = useState<string | "all" | null>(null);
  const [cancelling, setCancelling] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const cancelledRef = useRef(false);
  const settings = settingsQuery.data;
  const session = sessionQuery.data;
  const mailboxes = settings?.mailboxes ?? [];

  const checkMutation = useMutation({
    mutationFn: (mailboxId: string | null) =>
      runUntilCaughtUp(
        mailboxes,
        mailboxId,
        (nextSession) => {
          queryClient.setQueryData(mailCheckLastRunQueryKey, nextSession);
        },
        abortRef.current?.signal,
      ),
    onMutate: (mailboxId) => {
      cancelledRef.current = false;
      setCancelling(false);
      abortRef.current = new AbortController();
      setRunError(null);
      setActiveMailboxId(mailboxId ?? "all");
      const initial = createSession(mailboxes);
      queryClient.setQueryData(mailCheckLastRunQueryKey, {
        ...initial,
        phase: "scanning",
        message: mailboxId ? "Starting mailbox check…" : "Starting check on all mailboxes…",
      });
    },
    onSuccess: async (result) => {
      queryClient.setQueryData(mailCheckLastRunQueryKey, result);
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckInboxRootQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckNeedActionQueryKey });
      setActiveMailboxId(null);
      abortRef.current = null;
      setCancelling(false);
    },
    onError: (error) => {
      abortRef.current = null;
      setActiveMailboxId(null);
      setCancelling(false);
      if (isAbortError(error)) {
        return;
      }

      setRunError(errorMessage(error));
      queryClient.setQueryData(mailCheckLastRunQueryKey, null);
    },
  });

  function cancelCheck() {
    cancelledRef.current = true;
    setCancelling(true);
    abortRef.current?.abort();
  }

  const ready = Boolean(mailboxes.length > 0 && settings?.hasApiKey);
  const checking = checkMutation.isPending;
  const hasMailbox = mailboxes.length > 0;
  const totals = session?.totals;
  const items = totals?.items ?? [];
  const showSessionBanner = Boolean(session?.message && (checking || session.phase === "done" || session.phase === "cancelled"));
  const hasSessionStats = Boolean(session && (checking || session.totals.processed > 0));
  const sessionTotals = session?.totals;

  return (
    <Stack spacing={2}>
      {!hasMailbox ? (
        <Alert severity="info">Connect a mailbox on the Settings tab before checking mail.</Alert>
      ) : null}
      {hasMailbox && !settings?.hasApiKey ? (
        <Alert severity="info">Save an OpenAI API key on the Settings tab. That is the only paid piece.</Alert>
      ) : null}
      {runError ? <Alert severity="error">{runError}</Alert> : null}

      {hasSessionStats && sessionTotals ? (
        <Panel>
          <Stack spacing={1.5}>
            <Stack spacing={0.25}>
              <Typography variant="h6" component="h2">
                This check session
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Totals since you clicked Check or Check all. Cleared when you start a new check.
              </Typography>
            </Stack>
            <SessionStatsRow
              processed={sessionTotals.processed}
              labeled={sessionTotals.labeled}
              trashed={sessionTotals.trashed}
              skipped={sessionTotals.skipped}
              errors={sessionTotals.errors}
            />
          </Stack>
        </Panel>
      ) : null}

      {settings?.lastRunAt ? (
        <Panel>
          <Stack spacing={1.5}>
            <Stack spacing={0.25}>
              <Typography variant="h6" component="h2">
                Last background check
              </Typography>
              <Typography variant="body2" color="text.secondary">
                One message per auto-check while Mail Check is open (about every 2 minutes). Ran at{" "}
                {new Date(settings.lastRunAt).toLocaleString()}.
              </Typography>
            </Stack>
            <SessionStatsRow
              processed={settings.lastProcessed}
              labeled={settings.lastLabeled}
              trashed={settings.lastTrashed}
              skipped={settings.lastSkipped}
              errors={settings.lastErrors}
            />
            <Typography variant="caption" color="text.secondary">
              Lifetime pinned {settings.totalLabeled} · trashed {settings.totalTrashed}
            </Typography>
          </Stack>
        </Panel>
      ) : null}

      <Panel>
        <Stack spacing={2.5}>
          <Stack
            direction={{ xs: "column", sm: "row" }}
            spacing={2}
            sx={{ alignItems: { sm: "center" }, justifyContent: "space-between" }}
          >
            <Stack spacing={0.5} sx={{ flex: 1 }}>
              <Typography variant="h6" component="h2">
                Manual check
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Run Check or Check all to classify mail one message at a time. Counts below labeled
                &quot;This check session&quot; reset when you start a new check.
              </Typography>
            </Stack>
            <Button
              variant={checking ? "outlined" : "contained"}
              disabled={!ready && !checking}
              loading={checking && cancelling}
              onClick={() => {
                if (checking) {
                  cancelCheck();
                  return;
                }

                checkMutation.mutate(null);
              }}
            >
              {checking ? "Cancel" : "Check all"}
            </Button>
          </Stack>

          {checking ? (
            <Stack spacing={1}>
              <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
                <CircularProgress size={18} />
                <Typography variant="body2">{session?.message ?? "Working…"}</Typography>
              </Stack>
              <LinearProgress />
            </Stack>
          ) : null}

          {!checking && showSessionBanner && session?.message ? (
            <Alert severity={session.phase === "cancelled" ? "warning" : "success"}>{session.message}</Alert>
          ) : null}

          <Typography variant="subtitle2">Per mailbox · this check session</Typography>

          <Stack spacing={1.5}>
            {mailboxes.map((mailbox) => (
              <MailboxRunCard
                key={mailbox.id}
                mailbox={mailbox}
                stats={session?.mailboxStats[mailbox.id]}
                active={checking && session?.activeMailboxId === mailbox.id}
                checking={checking}
                isTarget={checking && (activeMailboxId === "all" || activeMailboxId === mailbox.id)}
                canRun={ready}
                showCancel={checking && (activeMailboxId === mailbox.id || (activeMailboxId === "all" && active))}
                cancelling={cancelling}
                onRun={() => checkMutation.mutate(mailbox.id)}
                onCancel={cancelCheck}
              />
            ))}
          </Stack>
        </Stack>
      </Panel>

      {items.length > 0 ? (
        <Panel>
          <Stack spacing={1.5}>
            <Typography variant="h6" component="h2">
              Recent decisions
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell align="left">Mailbox</TableCell>
                    <TableCell align="left">From</TableCell>
                    <TableCell align="left">Subject</TableCell>
                    <TableCell align="left">Label</TableCell>
                    <TableCell align="left">Action</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items
                    .slice()
                    .reverse()
                    .map((item, index) => (
                      <TableRow key={`${item.mailboxId}-${item.messageId}-${index}`}>
                        <TableCell align="left">{item.mailboxEmail}</TableCell>
                        <TableCell align="left">{item.from}</TableCell>
                        <TableCell align="left">{item.subject || "(no subject)"}</TableCell>
                        <TableCell align="left">{item.label}</TableCell>
                        <TableCell align="left">{actionLabel(item.action)}</TableCell>
                      </TableRow>
                    ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Stack>
        </Panel>
      ) : (
        <EmptyState>
          <Typography variant="body2" color="text.secondary">
            {checking
              ? "Decisions appear here as each message is classified."
              : "Run a check to see per-mailbox results and recent decisions."}
          </Typography>
        </EmptyState>
      )}
    </Stack>
  );
}

type MailboxRunCardProps = {
  mailbox: MailCheckMailboxItem;
  stats: MailboxCheckStats | undefined;
  active: boolean;
  checking: boolean;
  isTarget: boolean;
  canRun: boolean;
  showCancel: boolean;
  cancelling: boolean;
  onRun: () => void;
  onCancel: () => void;
};

function MailboxRunCard({
  mailbox,
  stats,
  active,
  checking,
  isTarget,
  canRun,
  showCancel,
  cancelling,
  onRun,
  onCancel,
}: MailboxRunCardProps) {
  const values = stats ?? {
    processed: 0,
    labeled: 0,
    trashed: 0,
    skipped: 0,
    errors: 0,
  };

  return (
    <MailboxCard active={active}>
      <Stack spacing={1.5}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={1.5}
          sx={{ alignItems: { sm: "center" }, justifyContent: "space-between" }}
        >
          <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", minWidth: 0 }}>
            {active ? <CircularProgress size={16} /> : null}
            <Stack spacing={0.25} sx={{ minWidth: 0 }}>
              <Typography variant="body2" noWrap>
                {providerLabel(mailbox.provider)} · {mailbox.email}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {formatMailboxScanStatus(mailbox)}
              </Typography>
              {active ? (
                <Typography variant="caption" color="primary">
                  Working
                </Typography>
              ) : checking && isTarget && !active ? (
                <Typography variant="caption" color="text.secondary">
                  Queued
                </Typography>
              ) : values.processed > 0 ? (
                <Typography variant="caption" color="text.secondary">
                  {values.processed} processed this session
                </Typography>
              ) : null}
            </Stack>
          </Stack>
          <Button
            variant="outlined"
            size="small"
            disabled={checking ? !showCancel : !canRun}
            loading={showCancel && cancelling}
            onClick={() => {
              if (showCancel) {
                onCancel();
                return;
              }

              onRun();
            }}
          >
            {showCancel ? "Cancel" : "Check"}
          </Button>
        </Stack>

        <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap", rowGap: 1.5 }}>
          <MailboxStat label="Processed" value={values.processed} emphasize={active} />
          <MailboxStat label="Pinned" value={values.labeled} emphasize={active && values.labeled > 0} />
          <MailboxStat label="Trashed" value={values.trashed} emphasize={active && values.trashed > 0} />
          <MailboxStat label="Kept" value={values.skipped} emphasize={active && values.skipped > 0} />
          <MailboxStat
            label="Errors"
            value={values.errors}
            emphasize={values.errors > 0}
            tone={values.errors > 0 ? "error" : "default"}
          />
        </Stack>
        {active ? <LinearProgress /> : null}
      </Stack>
    </MailboxCard>
  );
}

function SessionStatsRow({
  processed,
  labeled,
  trashed,
  skipped,
  errors,
}: {
  processed: number;
  labeled: number;
  trashed: number;
  skipped: number;
  errors: number;
}) {
  return (
    <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap", rowGap: 1.5 }}>
      <MailboxStat label="Processed" value={processed} />
      <MailboxStat label="Pinned" value={labeled} />
      <MailboxStat label="Trashed" value={trashed} />
      <MailboxStat label="Kept" value={skipped} />
      <MailboxStat label="Errors" value={errors} tone={errors > 0 ? "error" : "default"} />
    </Stack>
  );
}

function MailboxStat({
  label,
  value,
  emphasize = false,
  tone = "default",
}: {
  label: string;
  value: number;
  emphasize?: boolean;
  tone?: "default" | "error";
}) {
  return (
    <StatBlock>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      {tone === "error" && value > 0 ? (
        <Chip size="small" color="error" label={value} />
      ) : (
        <Typography variant="subtitle1" component="p" color={emphasize ? "primary" : "text.primary"}>
          {value}
        </Typography>
      )}
    </StatBlock>
  );
}
