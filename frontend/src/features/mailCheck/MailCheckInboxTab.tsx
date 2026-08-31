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
import { Link as RouterLink } from "react-router-dom";
import { EmptyState } from "@/features/mailCheck/mailCheckLayout";
import { errorMessage, gmailMessageUrl } from "@/features/mailCheck/mailCheckUi";
import { getMailCheckInbox, getMailCheckSettings, mailCheckInboxQueryKey, mailCheckSettingsQueryKey } from "@/shared/api/mailCheck";
import { appPaths } from "@/shared/config/paths";
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
  const inboxQuery = useQuery({
    queryKey: mailCheckInboxQueryKey(filter),
    queryFn: () => getMailCheckInbox(filter),
    enabled: settingsQuery.data?.gmailConnected === true,
    refetchInterval: 30_000,
  });
  const settings = settingsQuery.data;
  const items = inboxQuery.data?.items ?? [];

  return (
    <Stack spacing={2}>
      {settingsQuery.isError ? <Alert severity="error">{errorMessage(settingsQuery.error)}</Alert> : null}
      {inboxQuery.isError ? <Alert severity="error">{errorMessage(inboxQuery.error)}</Alert> : null}
      {settings && !settings.gmailConnected ? (
        <Alert severity="info">
          Connect Gmail on{" "}
          <Link component={RouterLink} to={appPaths.jobApplication}>
            Job Application
          </Link>{" "}
          Settings. Mail Check uses that same mailbox.
        </Alert>
      ) : null}
      {settings?.gmailConnected && !settings.hasApiKey ? (
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
      {inboxQuery.isPending && settings?.gmailConnected ? (
        <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
          <CircularProgress />
        </Box>
      ) : null}
      {settings?.gmailConnected && !inboxQuery.isPending && items.length === 0 ? (
        <EmptyState>
          <Typography variant="body2" color="text.secondary">
            No labeled mail yet. Open Check to run now, or wait for the auto check. Inbox, spam, and
            other Gmail categories are scanned.
          </Typography>
        </EmptyState>
      ) : null}
      {items.length > 0 ? (
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell align="left">From</TableCell>
                <TableCell align="left">Subject</TableCell>
                <TableCell align="left">Label</TableCell>
                <TableCell align="left">Date</TableCell>
                <TableCell align="left">Gmail</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {items.map((item) => (
                <TableRow key={item.id}>
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
                    <Link href={gmailMessageUrl(item.threadId, item.id)} target="_blank" rel="noopener noreferrer">
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
