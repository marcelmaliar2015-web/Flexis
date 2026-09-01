import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { EmptyState } from "@/features/mailCheck/mailCheckLayout";
import { mailboxMessageUrl, providerLabel } from "@/features/mailCheck/mailCheckUi";
import {
  getMailCheckInbox,
  getMailCheckSettings,
  mailCheckInboxQueryKey,
  mailCheckSettingsQueryKey,
} from "@/shared/api/mailCheck";
import { mailCheckKeepLabels, type MailCheckLabelSlug } from "@/shared/types/mailCheck";

const FilterRow = styled(Stack)(({ theme }) => ({
  flexWrap: "wrap",
  gap: theme.spacing(1),
}));

type InboxFilter = MailCheckLabelSlug | "all";

export function MailCheckInboxTab() {
  const [filter, setFilter] = useState<InboxFilter>("all");
  const settingsQuery = useQuery({
    queryKey: mailCheckSettingsQueryKey,
    queryFn: getMailCheckSettings,
  });
  const hasMailbox = (settingsQuery.data?.mailboxes.length ?? 0) > 0;
  const inboxQuery = useQuery({
    queryKey: mailCheckInboxQueryKey(filter),
    queryFn: () => getMailCheckInbox(filter),
    enabled: hasMailbox,
    refetchInterval: 30_000,
  });
  const settings = settingsQuery.data;
  const items = inboxQuery.data?.items ?? [];

  return (
    <Stack spacing={2}>
      {settings && !hasMailbox ? (
        <Alert severity="info">Connect a mailbox on the Settings tab to see labeled mail here.</Alert>
      ) : null}
      {hasMailbox && !settings?.hasApiKey ? (
        <Alert severity="info">Save an OpenAI API key on the Settings tab so Flexis can label new mail.</Alert>
      ) : null}
      <FilterRow direction="row">
        <Chip
          label="All"
          color={filter === "all" ? "primary" : "default"}
          onClick={() => setFilter("all")}
        />
        {mailCheckKeepLabels.map((item) => (
          <Chip
            key={item.slug}
            label={item.name}
            color={filter === item.slug ? "primary" : "default"}
            onClick={() => setFilter(item.slug)}
          />
        ))}
      </FilterRow>
      {inboxQuery.isPending && hasMailbox ? (
        <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
          <CircularProgress />
        </Box>
      ) : null}
      {hasMailbox && !inboxQuery.isPending && items.length === 0 ? (
        <EmptyState>
          <Typography variant="body2" color="text.secondary">
            No labeled mail yet. Open Check and click Check all, or wait for auto-check. Inbox and
            junk are scanned for Outlook; inbox, spam, and categories for Gmail. Every connected
            mailbox is included.
          </Typography>
        </EmptyState>
      ) : null}
      {items.length > 0 ? (
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell align="left">Mailbox</TableCell>
                <TableCell align="left">From</TableCell>
                <TableCell align="left">Subject</TableCell>
                <TableCell align="left">Label</TableCell>
                <TableCell align="left">Date</TableCell>
                <TableCell align="left">Open</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {items.map((item) => (
                <TableRow key={`${item.mailboxId}-${item.id}`}>
                  <TableCell align="left">
                    <Stack spacing={0.25}>
                      <Typography variant="body2">{providerLabel(item.mailboxProvider)}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {item.mailboxEmail}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell align="left">{item.from}</TableCell>
                  <TableCell align="left">
                    <Stack spacing={0.5}>
                      <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                        <Typography variant="body2">{item.subject || "(no subject)"}</Typography>
                        {item.starred ? <Chip size="small" label="Pinned" /> : null}
                      </Stack>
                      {item.snippet ? (
                        <Typography variant="caption" color="text.secondary">
                          {item.snippet}
                        </Typography>
                      ) : null}
                    </Stack>
                  </TableCell>
                  <TableCell align="left">{item.label}</TableCell>
                  <TableCell align="left">{item.date}</TableCell>
                  <TableCell align="left">
                    <Link
                      href={mailboxMessageUrl(item.mailboxProvider, item.threadId, item.id)}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      Open
                    </Link>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      ) : null}
    </Stack>
  );
}
