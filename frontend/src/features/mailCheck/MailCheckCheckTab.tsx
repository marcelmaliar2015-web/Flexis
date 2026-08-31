import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { EmptyState, Panel } from "@/features/mailCheck/mailCheckLayout";
import { actionLabel } from "@/features/mailCheck/mailCheckUi";
import {
  getMailCheckSettings,
  mailCheckInboxRootQueryKey,
  mailCheckLastRunQueryKey,
  mailCheckSettingsQueryKey,
  runMailCheck,
} from "@/shared/api/mailCheck";
import type { MailCheckRun } from "@/shared/types/mailCheck";

async function runUntilCaughtUp(): Promise<MailCheckRun> {
  let last = await runMailCheck(true);
  let rounds = 1;
  while (last.hasMore && !last.busy && rounds < 8) {
    const next = await runMailCheck(true);
    last = {
      ...next,
      processed: last.processed + next.processed,
      labeled: last.labeled + next.labeled,
      trashed: last.trashed + next.trashed,
      skipped: last.skipped + next.skipped,
      errors: last.errors + next.errors,
      items: [...last.items, ...next.items],
    };
    rounds += 1;
  }
  return last;
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
  const settings = settingsQuery.data;
  const lastRun = lastRunQuery.data;

  const checkMutation = useMutation({
    mutationFn: runUntilCaughtUp,
    onSuccess: async (result) => {
      queryClient.setQueryData(mailCheckLastRunQueryKey, result);
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckInboxRootQueryKey });
    },
  });

  const ready = Boolean(settings?.mailboxConnected && settings.hasApiKey);
  const stats = lastRun ?? {
    processed: settings?.lastProcessed ?? 0,
    labeled: settings?.lastLabeled ?? 0,
    trashed: settings?.lastTrashed ?? 0,
    skipped: settings?.lastSkipped ?? 0,
    errors: settings?.lastErrors ?? 0,
  };

  return (
    <Stack spacing={2}>
      {!settings?.mailboxConnected ? (
        <Alert severity="info">Connect a mailbox on the Settings tab before checking mail.</Alert>
      ) : null}
      {settings?.mailboxConnected && !settings.hasApiKey ? (
        <Alert severity="info">Save an OpenAI API key on the Settings tab. That is the only paid piece.</Alert>
      ) : null}
      <Panel>
        <Stack spacing={2}>
          <Stack
            direction={{ xs: "column", sm: "row" }}
            spacing={2}
            sx={{ alignItems: { sm: "center" }, justifyContent: "space-between" }}
          >
            <Stack spacing={0.5}>
              <Typography variant="h6" component="h2">
                Auto mail check
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Flexis scans inbox, spam, and other Gmail categories about every two minutes while
                this browser tab is visible. It labels interview mail, pins it, and trashes
                application receipts. Personal mail is left alone.
              </Typography>
            </Stack>
            <Button
              disabled={!ready || checkMutation.isPending}
              loading={checkMutation.isPending}
              onClick={() => checkMutation.mutate()}
            >
              Check now
            </Button>
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {settings?.lastRunAt
              ? `Last run ${new Date(settings.lastRunAt).toLocaleString()}`
              : "No check has run yet."}
          </Typography>
        </Stack>
      </Panel>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
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
                <TableCell align="left">From</TableCell>
                <TableCell align="left">Subject</TableCell>
                <TableCell align="left">Action</TableCell>
                <TableCell align="left">Why</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {lastRun.items.map((item, index) => (
                <TableRow key={`${item.gmailMessageId}-${index}`}>
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
            Decisions from the latest check in this session appear here.
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
