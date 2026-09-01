import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import LinearProgress from "@mui/material/LinearProgress";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { EmptyState, Panel } from "@/features/mailCheck/mailCheckLayout";
import { actionLabel, errorMessage } from "@/features/mailCheck/mailCheckUi";
import {
  getMailCheckSettings,
  mailCheckInboxRootQueryKey,
  mailCheckLastRunQueryKey,
  mailCheckSettingsQueryKey,
  runMailCheck,
} from "@/shared/api/mailCheck";
import type { MailCheckRun } from "@/shared/types/mailCheck";

const maxRounds = 500;
const busyWaitMs = 1500;
const maxBusyRetries = 20;

function emptyRun(): MailCheckRun {
  return {
    busy: false,
    processed: 0,
    labeled: 0,
    trashed: 0,
    skipped: 0,
    errors: 0,
    hasMore: false,
    items: [],
  };
}

function mergeRuns(previous: MailCheckRun, next: MailCheckRun): MailCheckRun {
  return {
    ...next,
    processed: previous.processed + next.processed,
    labeled: previous.labeled + next.labeled,
    trashed: previous.trashed + next.trashed,
    skipped: previous.skipped + next.skipped,
    errors: previous.errors + next.errors,
    items: [...previous.items, ...next.items],
  };
}

function wait(ms: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });
}

async function runUntilCaughtUp(
  onProgress: (run: MailCheckRun, round: number, status: string) => void,
): Promise<MailCheckRun> {
  let total = emptyRun();
  let rounds = 0;
  let busyRetries = 0;

  onProgress(total, 0, "Starting check. Reading mailbox and classifying messages…");

  while (rounds < maxRounds) {
    const next = await runMailCheck(true);
    if (next.busy) {
      busyRetries += 1;
      if (busyRetries > maxBusyRetries) {
        onProgress(total, rounds, "Another check is still running. Wait a moment and try again.");
        return { ...total, busy: true, hasMore: true };
      }

      onProgress(
        total,
        rounds,
        "Waiting for the current mailbox check to finish…",
      );
      await wait(busyWaitMs);
      continue;
    }

    busyRetries = 0;
    rounds += 1;
    total = mergeRuns(total, next);
    onProgress(
      total,
      rounds,
      next.hasMore
        ? `Batch ${rounds} finished. More mail remains. Continuing…`
        : `Finished. Processed ${total.processed} message(s) in ${rounds} batch(es).`,
    );

    if (!next.hasMore) {
      break;
    }
  }

  if (total.hasMore && rounds >= maxRounds) {
    onProgress(
      total,
      rounds,
      `Stopped after ${maxRounds} batches. Click Check all again to continue.`,
    );
  }

  return total;
}

export function MailCheckCheckTab() {
  const queryClient = useQueryClient();
  const settingsQuery = useQuery({
    queryKey: mailCheckSettingsQueryKey,
    queryFn: getMailCheckSettings,
  });
  const lastRunQuery = useQuery({
    queryKey: mailCheckLastRunQueryKey,
    queryFn: async () => null as MailCheckRun | null,
    enabled: false,
    staleTime: Infinity,
  });
  const [progressText, setProgressText] = useState<string | null>(null);
  const [runError, setRunError] = useState<string | null>(null);
  const settings = settingsQuery.data;
  const lastRun = lastRunQuery.data;

  const checkMutation = useMutation({
    mutationFn: () =>
      runUntilCaughtUp((run, _round, status) => {
        setProgressText(status);
        queryClient.setQueryData(mailCheckLastRunQueryKey, run);
      }),
    onMutate: () => {
      setRunError(null);
      setProgressText("Starting Check all. Reading mailbox and classifying messages…");
    },
    onSuccess: async (result) => {
      queryClient.setQueryData(mailCheckLastRunQueryKey, result);
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckInboxRootQueryKey });
      if (result.busy && result.processed === 0) {
        setProgressText("Another check is still running. Wait a moment and try again.");
        return;
      }

      if (result.hasMore) {
        setProgressText(
          `Processed ${result.processed} message(s). More mail remains. Click Check all again to continue.`,
        );
        return;
      }

      setProgressText(
        result.processed === 0
          ? "Check all finished. No new messages needed action."
          : `Check all finished. Processed ${result.processed} message(s).`,
      );
    },
    onError: (error) => {
      setRunError(errorMessage(error));
      setProgressText(null);
    },
  });

  const ready = Boolean((settings?.mailboxes.length ?? 0) > 0 && settings?.hasApiKey);
  const checking = checkMutation.isPending;
  const stats = lastRun ?? {
    processed: settings?.lastProcessed ?? 0,
    labeled: settings?.lastLabeled ?? 0,
    trashed: settings?.lastTrashed ?? 0,
    skipped: settings?.lastSkipped ?? 0,
    errors: settings?.lastErrors ?? 0,
  };
  const hasMailbox = (settings?.mailboxes.length ?? 0) > 0;

  return (
    <Stack spacing={2}>
      {!hasMailbox ? (
        <Alert severity="info">Connect a mailbox on the Settings tab before checking mail.</Alert>
      ) : null}
      {hasMailbox && !settings?.hasApiKey ? (
        <Alert severity="info">Save an OpenAI API key on the Settings tab. That is the only paid piece.</Alert>
      ) : null}
      {runError ? <Alert severity="error">{runError}</Alert> : null}
      <Panel>
        <Stack spacing={2}>
          <Stack
            direction={{ xs: "column", sm: "row" }}
            spacing={2}
            sx={{ alignItems: { sm: "flex-start" }, justifyContent: "space-between" }}
          >
            <Stack spacing={1} sx={{ flex: 1 }}>
              <Typography variant="h6" component="h2">
                Check all
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Check all scans every connected mailbox: inbox and junk (Outlook) or inbox, spam, and
                Gmail categories until caught up. Already-checked messages are skipped. Sent, drafts,
                and archive are not scanned. Keep this page open. Progress updates as each batch
                finishes.
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Background auto-check still runs about every two minutes while Mail Check is open.
                Use Check all when you want the full catch-up now.
              </Typography>
            </Stack>
            <Button
              disabled={!ready || checking}
              loading={checking}
              onClick={() => {
                if (checking) {
                  return;
                }

                checkMutation.mutate();
              }}
            >
              {checking ? "Checking all…" : "Check all"}
            </Button>
          </Stack>
          {checking ? <LinearProgress /> : null}
          {progressText ? (
            <Alert severity={checking ? "info" : "success"}>{progressText}</Alert>
          ) : null}
          <Typography variant="body2" color="text.secondary">
            {settings?.lastRunAt
              ? `Last finished run ${new Date(settings.lastRunAt).toLocaleString()}`
              : "No check has finished yet."}
          </Typography>
        </Stack>
      </Panel>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
        <Box sx={{ flex: 1 }}>
          <StatCard label="Processed" value={stats.processed} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <StatCard label="Labeled" value={stats.labeled} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <StatCard label="Trashed" value={stats.trashed} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <StatCard label="Left in place" value={stats.skipped} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <StatCard label="Errors" value={stats.errors} />
        </Box>
      </Stack>
      {lastRun && lastRun.items.length > 0 ? (
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell align="left">Mailbox</TableCell>
                <TableCell align="left">From</TableCell>
                <TableCell align="left">Subject</TableCell>
                <TableCell align="left">Action</TableCell>
                <TableCell align="left">Why</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {lastRun.items.map((item, index) => (
                <TableRow key={`${item.mailboxId}-${item.messageId}-${index}`}>
                  <TableCell align="left">{item.mailboxEmail}</TableCell>
                  <TableCell align="left">{item.from}</TableCell>
                  <TableCell align="left">{item.subject || "(no subject)"}</TableCell>
                  <TableCell align="left">{item.label || actionLabel(item.action)}</TableCell>
                  <TableCell align="left">{item.reason}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      ) : (
        <EmptyState>
          <Typography variant="body2" color="text.secondary">
            {checking
              ? "Working… message decisions will appear here as each batch finishes."
              : "Click Check all to process mail. Decisions from this session appear here."}
          </Typography>
        </EmptyState>
      )}
    </Stack>
  );
}

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <Panel>
      <Stack spacing={0.5}>
        <Typography variant="overline" color="secondary">
          {label}
        </Typography>
        <Typography variant="h4" component="p">
          {value}
        </Typography>
      </Stack>
    </Panel>
  );
}
