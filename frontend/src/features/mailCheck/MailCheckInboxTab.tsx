import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { MailCheckInboxTable } from "@/features/mailCheck/MailCheckInboxTable";
import { EmptyState, Panel } from "@/features/mailCheck/mailCheckLayout";
import {
  getMailCheckInbox,
  getMailCheckSettings,
  mailCheckInboxQueryKey,
  mailCheckSettingsQueryKey,
} from "@/shared/api/mailCheck";
import { mailCheckPinLabelFilters, type MailCheckLabelSlug } from "@/shared/types/mailCheck";

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
  const pinFilters = mailCheckPinLabelFilters(settings?.labelActions);

  return (
    <Stack spacing={2}>
      {settings && !hasMailbox ? (
        <Alert severity="info">Connect a mailbox on the Settings tab to see labeled mail here.</Alert>
      ) : null}
      {hasMailbox && !settings?.hasApiKey ? (
        <Alert severity="info">Save an OpenAI API key on the Settings tab so Flexis can label new mail.</Alert>
      ) : null}
      <Panel>
        <Stack spacing={1.5}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Pinned mail
            </Typography>
            <Typography variant="body2" color="text.secondary">
              All pinned messages in your connected mailboxes. This is a live view from Gmail or
              Outlook, not limited to a date range.
            </Typography>
          </Stack>
          <FilterRow direction="row">
            <Chip
              label="All"
              color={filter === "all" ? "primary" : "default"}
              onClick={() => setFilter("all")}
            />
            {pinFilters.map((item) => (
              <Chip
                key={item.slug}
                label={item.name}
                color={filter === item.slug ? "primary" : "default"}
                onClick={() => setFilter(item.slug)}
              />
            ))}
          </FilterRow>
        </Stack>
      </Panel>
      {inboxQuery.isPending && hasMailbox ? (
        <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
          <CircularProgress />
        </Box>
      ) : null}
      {hasMailbox && !inboxQuery.isPending && items.length === 0 ? (
        <EmptyState>
          <Typography variant="body2" color="text.secondary">
            No pinned mail yet. Open Check and run a check, or wait for auto-check while this page
            is open.
          </Typography>
        </EmptyState>
      ) : null}
      {items.length > 0 ? <MailCheckInboxTable items={items} /> : null}
    </Stack>
  );
}
