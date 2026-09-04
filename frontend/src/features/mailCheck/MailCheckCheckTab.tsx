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
import { useMailCheckAuto } from "@/app/providers/MailCheckProvider";
import { MailCheckProgressPanel } from "@/features/mailCheck/MailCheckProgressPanel";
import { EmptyState, Panel } from "@/features/mailCheck/mailCheckLayout";
import type { MailboxCheckStats } from "@/features/mailCheck/mailCheckRunSession";
import { actionLabel, formatMailboxScanStatus, providerLabel } from "@/features/mailCheck/mailCheckUi";
import type { useMailCheckRun } from "@/features/mailCheck/useMailCheckRun";
import type { MailCheckMailboxItem, MailCheckSettings } from "@/shared/types/mailCheck";

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

type MailCheckRunState = ReturnType<typeof useMailCheckRun>;

type MailCheckCheckTabProps = {
  settings: MailCheckSettings | undefined;
  mailCheckRun: MailCheckRunState;
};

export function MailCheckCheckTab({ settings, mailCheckRun }: MailCheckCheckTabProps) {
  const autoCheck = useMailCheckAuto();
  const {
    session,
    runError,
    activeMailboxId,
    cancelling,
    checking,
    elapsedMs,
    serverWaitMs,
    liveProgress,
    interruptedByRefresh,
    cancelCheck,
    startCheck,
  } = mailCheckRun;
  const mailboxes = settings?.mailboxes ?? [];
  const ready = Boolean(mailboxes.length > 0 && settings?.hasApiKey);
  const hasMailbox = mailboxes.length > 0;
  const totals = session?.totals;
  const items = totals?.items ?? [];
  const showSessionBanner = Boolean(
    session?.message && !checking && (session.phase === "done" || session.phase === "cancelled"),
  );
  const showProgress = Boolean(
    session &&
      (checking ||
        interruptedByRefresh ||
        session.totals.processed > 0 ||
        session.totals.scanned > 0 ||
        session.phase === "done" ||
        session.phase === "cancelled"),
  );
  const sessionTotals = session?.totals;
  const activeMailboxKey =
    session?.activeMailboxId ?? (activeMailboxId === "all" ? null : activeMailboxId);

  return (
    <Stack spacing={2}>
      {!hasMailbox ? (
        <Alert severity="info">Connect a mailbox on Settings before checking mail.</Alert>
      ) : null}
      {hasMailbox && !settings?.hasApiKey ? (
        <Alert severity="info">Save an OpenAI API key on Settings. That is the only paid piece.</Alert>
      ) : null}
      {runError ? <Alert severity="error">{runError}</Alert> : null}

      {autoCheck.isLive ? (
        <Alert severity="info">
          Auto-check runs on the API every {autoCheck.intervalSeconds} seconds while it stays enabled
          {autoCheck.isRunning ? " and is classifying mail now" : ""}. It continues even if this tab
          or browser is closed. The AppBar pill shows live status. Manual Check takes the server
          lock ahead of the background auto-check.
        </Alert>
      ) : null}

      {showProgress && session && sessionTotals ? (
        <Panel>
          <Stack spacing={1.5}>
            <MailCheckProgressPanel
              session={session}
              checking={checking}
              elapsedMs={elapsedMs}
              serverWaitMs={serverWaitMs}
              liveProgress={liveProgress}
              autoCheckLive={autoCheck.isLive}
              interruptedByRefresh={interruptedByRefresh}
            />
            <SessionStatsRow
              processed={
                sessionTotals.processed +
                (liveProgress?.active && !liveProgress.waitingForLock ? liveProgress.processed : 0)
              }
              labeled={sessionTotals.labeled}
              trashed={sessionTotals.trashed}
              skipped={sessionTotals.skipped}
              alreadySeen={
                sessionTotals.alreadySeen +
                (liveProgress?.active && !liveProgress.waitingForLock ? liveProgress.alreadySeen : 0)
              }
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
                Up to three messages per auto-check API call (every{" "}
                {settings.autoCheckIntervalSeconds ?? 20} seconds). Ran at{" "}
                {new Date(settings.lastRunAt).toLocaleString()}. Trashed mail is in Gmail Trash or
                Outlook Deleted Items. Left in inbox mail stays in the inbox with a Flexis category
                or label.
              </Typography>
            </Stack>
            <SessionStatsRow
              processed={settings.lastProcessed}
              labeled={settings.lastLabeled}
              trashed={settings.lastTrashed}
              skipped={settings.lastSkipped}
              alreadySeen={0}
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
                Newest unprocessed message first. No date limit. Outlook sorts by received time;
                Gmail skips mail that already has a Flexis label. Each server round handles up to
                three messages; Check all repeats until caught up. Trashed moves mail to Gmail Trash
                or Outlook Deleted Items. Left in inbox keeps mail in place with a Flexis label or
                category. Open the progress panel above for live stages and timing.
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

                startCheck(null);
              }}
            >
              {checking ? "Cancel" : "Check all"}
            </Button>
          </Stack>

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
                active={checking && activeMailboxKey === mailbox.id}
                checking={checking}
                isTarget={checking && (activeMailboxId === "all" || activeMailboxId === mailbox.id)}
                canRun={ready}
                showCancel={checking && (activeMailboxId === mailbox.id || (activeMailboxId === "all" && activeMailboxKey === mailbox.id))}
                cancelling={cancelling}
                onRun={() => startCheck(mailbox.id)}
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
    alreadySeen: 0,
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
          <MailboxStat label="Left in inbox" value={values.skipped} emphasize={active && values.skipped > 0} />
          <MailboxStat
            label="Already labeled"
            value={values.alreadySeen}
            emphasize={active && values.alreadySeen > 0}
          />
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
  alreadySeen,
  errors,
}: {
  processed: number;
  labeled: number;
  trashed: number;
  skipped: number;
  alreadySeen: number;
  errors: number;
}) {
  return (
    <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap", rowGap: 1.5 }}>
      <MailboxStat label="Processed" value={processed} />
      <MailboxStat label="Pinned" value={labeled} />
      <MailboxStat label="Trashed" value={trashed} />
      <MailboxStat label="Left in inbox" value={skipped} />
      <MailboxStat label="Already labeled" value={alreadySeen} />
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
