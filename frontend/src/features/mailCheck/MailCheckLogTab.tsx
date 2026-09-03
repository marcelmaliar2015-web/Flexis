import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import FormControl from "@mui/material/FormControl";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import Select from "@mui/material/Select";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TablePagination from "@mui/material/TablePagination";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { EmptyState, Panel } from "@/features/mailCheck/mailCheckLayout";
import { actionLabel, providerLabel } from "@/features/mailCheck/mailCheckUi";
import {
  getMailCheckSettings,
  listMailCheckLogs,
  mailCheckLogsQueryKey,
  mailCheckSettingsQueryKey,
} from "@/shared/api/mailCheck";
import type {
  MailCheckActionLog,
  MailCheckActionLogQuery,
  MailCheckLogSource,
} from "@/shared/types/mailCheck";

const FilterRow = styled(Stack)(({ theme }) => ({
  flexWrap: "wrap",
  gap: theme.spacing(1),
}));

const Toolbar = styled(Stack)(({ theme }) => ({
  gap: theme.spacing(2),
}));

const LogTableContainer = styled(TableContainer)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  borderRadius: theme.shape.borderRadius,
  maxHeight: 640,
  backgroundColor: theme.palette.background.paper,
}));

const StickyHeadCell = styled(TableCell)(({ theme }) => ({
  position: "sticky",
  top: 0,
  zIndex: 2,
  backgroundColor: theme.palette.background.paper,
  fontWeight: 600,
  whiteSpace: "nowrap",
}));

const MonoCaption = styled(Typography)(({ theme }) => ({
  fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace",
  fontSize: theme.typography.caption.fontSize,
  color: theme.palette.text.secondary,
}));

const sourceFilters: { value: MailCheckLogSource | "all"; label: string }[] = [
  { value: "all", label: "All sources" },
  { value: "auto", label: "Auto-check" },
  { value: "manual", label: "Manual check" },
];

const actionFilters: { value: string; label: string }[] = [
  { value: "all", label: "All actions" },
  { value: "pin", label: "Pinned" },
  { value: "trash", label: "Trashed" },
  { value: "keep", label: "Left in inbox" },
  { value: "already_checked", label: "Already labeled" },
  { value: "error", label: "Errors" },
  { value: "run_completed", label: "Run summaries" },
];

const pageSizeOptions = [25, 50, 100];

function formatWhen(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    second: "2-digit",
  }).format(new Date(value));
}

function formatDuration(ms: number): string {
  if (ms <= 0) {
    return "—";
  }
  if (ms < 1000) {
    return `${ms} ms`;
  }
  if (ms < 60_000) {
    return `${(ms / 1000).toFixed(1)} s`;
  }
  const minutes = Math.floor(ms / 60_000);
  const seconds = ((ms % 60_000) / 1000).toFixed(0);
  return `${minutes}m ${seconds}s`;
}

function sourceLabel(source: string): string {
  if (source === "auto") {
    return "Auto";
  }
  if (source === "manual") {
    return "Manual";
  }
  return source;
}

function sourceColor(source: string): "secondary" | "primary" | "default" {
  if (source === "auto") {
    return "secondary";
  }
  if (source === "manual") {
    return "primary";
  }
  return "default";
}

function actionColor(action: string): "success" | "error" | "warning" | "info" | "default" {
  if (action === "pin") {
    return "success";
  }
  if (action === "trash") {
    return "warning";
  }
  if (action === "error") {
    return "error";
  }
  if (action === "run_completed") {
    return "info";
  }
  return "default";
}

function logActionLabel(action: string): string {
  if (action === "run_completed") {
    return "Run completed";
  }
  return actionLabel(action);
}

function isRunSummary(item: MailCheckActionLog): boolean {
  return item.action === "run_completed" || item.messageId.length === 0;
}

function useDebouncedValue(value: string, delayMs: number): string {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const id = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(id);
  }, [delayMs, value]);
  return debounced;
}

export function MailCheckLogTab() {
  const settingsQuery = useQuery({
    queryKey: mailCheckSettingsQueryKey,
    queryFn: getMailCheckSettings,
  });
  const mailboxes = settingsQuery.data?.mailboxes ?? [];
  const [source, setSource] = useState<MailCheckLogSource | "all">("all");
  const [action, setAction] = useState("all");
  const [mailboxId, setMailboxId] = useState<string>("all");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(50);
  const debouncedSearch = useDebouncedValue(search, 300);

  useEffect(() => {
    setPage(0);
  }, [source, action, mailboxId, debouncedSearch, pageSize]);

  const query: MailCheckActionLogQuery = useMemo(
    () => ({
      page: page + 1,
      pageSize,
      source,
      action,
      mailboxId: mailboxId === "all" ? null : mailboxId,
      q: debouncedSearch,
    }),
    [action, debouncedSearch, mailboxId, page, pageSize, source],
  );

  const logsQuery = useQuery({
    queryKey: mailCheckLogsQueryKey(query),
    queryFn: () => listMailCheckLogs(query),
    placeholderData: keepPreviousData,
    refetchInterval: 30_000,
  });

  const items = logsQuery.data?.items ?? [];
  const totalCount = logsQuery.data?.totalCount ?? 0;
  const hasFilters =
    source !== "all" || action !== "all" || mailboxId !== "all" || debouncedSearch.trim().length > 0;

  return (
    <Stack spacing={2.5}>
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ alignItems: { md: "flex-start" }, justifyContent: "space-between" }}
      >
        <Stack spacing={0.5} sx={{ minWidth: 0 }}>
          <Typography variant="h6" component="h2">
            Activity log
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Durable history of auto-check and manual check actions across every mailbox. Filter by
            source, action, or mailbox, then search subject, sender, or detail.
          </Typography>
        </Stack>
        <Button
          variant="text"
          onClick={() => void logsQuery.refetch()}
          disabled={logsQuery.isFetching}
          sx={{ alignSelf: { xs: "stretch", md: "flex-start" }, flexShrink: 0 }}
        >
          Refresh
        </Button>
      </Stack>

      <Panel>
        <Toolbar>
          <TextField
            fullWidth
            size="small"
            label="Search logs"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Subject, sender, mailbox, label, detail, or message id"
          />
          <FilterRow direction="row">
            {sourceFilters.map((filter) => (
              <Chip
                key={filter.value}
                label={filter.label}
                color={source === filter.value ? "primary" : "default"}
                variant={source === filter.value ? "filled" : "outlined"}
                onClick={() => setSource(filter.value)}
              />
            ))}
          </FilterRow>
          <FilterRow direction="row">
            {actionFilters.map((filter) => (
              <Chip
                key={filter.value}
                label={filter.label}
                color={action === filter.value ? "secondary" : "default"}
                variant={action === filter.value ? "filled" : "outlined"}
                onClick={() => setAction(filter.value)}
              />
            ))}
          </FilterRow>
          {mailboxes.length > 1 ? (
            <FormControl size="small" sx={{ minWidth: 260, maxWidth: 420 }}>
              <InputLabel id="mail-check-log-mailbox-label">Mailbox</InputLabel>
              <Select
                labelId="mail-check-log-mailbox-label"
                label="Mailbox"
                value={mailboxId}
                onChange={(event) => setMailboxId(event.target.value)}
              >
                <MenuItem value="all">All mailboxes</MenuItem>
                {mailboxes.map((mailbox) => (
                  <MenuItem key={mailbox.id} value={mailbox.id}>
                    {providerLabel(mailbox.provider)} · {mailbox.email}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          ) : null}
          <Stack
            direction="row"
            spacing={1}
            useFlexGap
            sx={{ alignItems: "center", justifyContent: "space-between", flexWrap: "wrap" }}
          >
            <Typography variant="caption" color="text.secondary">
              {logsQuery.isFetching && !logsQuery.isPending
                ? "Updating…"
                : totalCount === 1
                  ? "1 event"
                  : `${totalCount.toLocaleString()} events`}
              {hasFilters ? " matching filters" : ""}
            </Typography>
            {hasFilters ? (
              <Button
                size="small"
                variant="text"
                onClick={() => {
                  setSource("all");
                  setAction("all");
                  setMailboxId("all");
                  setSearch("");
                }}
              >
                Clear filters
              </Button>
            ) : null}
          </Stack>
        </Toolbar>
      </Panel>

      {logsQuery.isPending ? (
        <Stack spacing={1} sx={{ alignItems: "center", py: 8 }}>
          <CircularProgress size={28} />
          <Typography variant="body2" color="text.secondary">
            Loading activity…
          </Typography>
        </Stack>
      ) : null}

      {logsQuery.isSuccess && totalCount === 0 && !hasFilters ? (
        <EmptyState>
          <Typography variant="subtitle1">No Mail Check activity yet</Typography>
          <Typography variant="body2" color="text.secondary">
            Auto-check and manual Check runs will record each message action here with source,
            mailbox, subject, label, action, and duration.
          </Typography>
        </EmptyState>
      ) : null}

      {logsQuery.isSuccess && totalCount === 0 && hasFilters ? (
        <EmptyState>
          <Typography variant="subtitle1">No matching events</Typography>
          <Typography variant="body2" color="text.secondary">
            Try another source, action, mailbox, or a broader search.
          </Typography>
        </EmptyState>
      ) : null}

      {logsQuery.isSuccess && items.length > 0 ? (
        <Box>
          <LogTableContainer>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <StickyHeadCell align="left">When</StickyHeadCell>
                  <StickyHeadCell align="left">Source</StickyHeadCell>
                  <StickyHeadCell align="left">Mailbox</StickyHeadCell>
                  <StickyHeadCell align="left">From</StickyHeadCell>
                  <StickyHeadCell align="left">Subject</StickyHeadCell>
                  <StickyHeadCell align="left">Label</StickyHeadCell>
                  <StickyHeadCell align="left">Action</StickyHeadCell>
                  <StickyHeadCell align="left">Duration</StickyHeadCell>
                  <StickyHeadCell align="left">Detail</StickyHeadCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {items.map((item) => {
                  const runRow = isRunSummary(item);
                  return (
                    <TableRow
                      key={item.id}
                      hover
                      sx={{
                        bgcolor: runRow ? "action.hover" : undefined,
                        "& td": { verticalAlign: "top" },
                      }}
                    >
                      <TableCell align="left" sx={{ whiteSpace: "nowrap" }}>
                        <Typography variant="body2">{formatWhen(item.occurredAt)}</Typography>
                        <Tooltip title={`Run ${item.runId}`}>
                          <MonoCaption>#{item.runId.slice(0, 8)}</MonoCaption>
                        </Tooltip>
                      </TableCell>
                      <TableCell align="left">
                        <Chip
                          size="small"
                          label={sourceLabel(item.source)}
                          color={sourceColor(item.source)}
                          variant={item.source === "auto" ? "filled" : "outlined"}
                        />
                      </TableCell>
                      <TableCell align="left" sx={{ maxWidth: 180 }}>
                        {item.mailboxEmail ? (
                          <Stack spacing={0.25}>
                            <Typography variant="body2">
                              {providerLabel(item.mailboxProvider)}
                            </Typography>
                            <Typography variant="caption" color="text.secondary" noWrap>
                              {item.mailboxEmail}
                            </Typography>
                          </Stack>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            {runRow ? "All mailboxes in round" : "—"}
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell align="left" sx={{ maxWidth: 180 }}>
                        <Typography variant="body2" noWrap title={item.from}>
                          {runRow ? "—" : item.from || "(unknown)"}
                        </Typography>
                      </TableCell>
                      <TableCell align="left" sx={{ maxWidth: 260 }}>
                        <Typography variant="body2" title={item.subject}>
                          {runRow ? "Server round summary" : item.subject || "(no subject)"}
                        </Typography>
                        {!runRow && item.messageId ? (
                          <MonoCaption noWrap title={item.messageId}>
                            {item.messageId.length > 28
                              ? `${item.messageId.slice(0, 28)}…`
                              : item.messageId}
                          </MonoCaption>
                        ) : null}
                      </TableCell>
                      <TableCell align="left">
                        {item.label ? (
                          <Chip size="small" variant="outlined" label={item.label} />
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            —
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell align="left">
                        <Chip
                          size="small"
                          label={logActionLabel(item.action)}
                          color={actionColor(item.action)}
                          variant={item.action === "error" ? "filled" : "outlined"}
                        />
                      </TableCell>
                      <TableCell align="left" sx={{ whiteSpace: "nowrap" }}>
                        <Typography variant="body2">{formatDuration(item.durationMs)}</Typography>
                      </TableCell>
                      <TableCell align="left" sx={{ maxWidth: 320 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          title={item.detail}
                          sx={{
                            display: "-webkit-box",
                            WebkitLineClamp: 3,
                            WebkitBoxOrient: "vertical",
                            overflow: "hidden",
                          }}
                        >
                          {item.detail || "—"}
                        </Typography>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </LogTableContainer>
          <TablePagination
            component="div"
            count={totalCount}
            page={page}
            onPageChange={(_event, nextPage) => setPage(nextPage)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(event) => {
              setPageSize(Number.parseInt(event.target.value, 10));
              setPage(0);
            }}
            rowsPerPageOptions={pageSizeOptions}
            labelRowsPerPage="Rows"
          />
        </Box>
      ) : null}
    </Stack>
  );
}
