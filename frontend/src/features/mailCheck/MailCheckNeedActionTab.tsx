import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { MailCheckInboxTable } from "@/features/mailCheck/MailCheckInboxTable";
import { EmptyState, Panel } from "@/features/mailCheck/mailCheckLayout";
import {
  getMailCheckNeedAction,
  getMailCheckSettings,
  mailCheckNeedActionQueryKey,
  mailCheckSettingsQueryKey,
} from "@/shared/api/mailCheck";
import { mailCheckLabels, type MailCheckLabelSlug } from "@/shared/types/mailCheck";

const FilterRow = styled(Stack)(({ theme }) => ({
  flexWrap: "wrap",
  gap: theme.spacing(1),
}));

function labelName(slug: MailCheckLabelSlug): string {
  return mailCheckLabels.find((item) => item.slug === slug)?.name ?? slug;
}

export function MailCheckNeedActionTab() {
  const [filter, setFilter] = useState<MailCheckLabelSlug | "all">("all");
  const settingsQuery = useQuery({
    queryKey: mailCheckSettingsQueryKey,
    queryFn: getMailCheckSettings,
  });
  const hasMailbox = (settingsQuery.data?.mailboxes.length ?? 0) > 0;
  const needActionQuery = useQuery({
    queryKey: mailCheckNeedActionQueryKey,
    queryFn: getMailCheckNeedAction,
    enabled: hasMailbox,
    refetchInterval: 30_000,
  });
  const settings = settingsQuery.data;
  const configured = settings?.needActionLabels ?? [];
  const allItems = needActionQuery.data?.items ?? [];
  const items = useMemo(() => {
    if (filter === "all") {
      return allItems;
    }

    return allItems.filter((item) => item.labelSlug === filter);
  }, [allItems, filter]);

  return (
    <Stack spacing={2}>
      {settings && !hasMailbox ? (
        <Alert severity="info">Connect a mailbox on the Settings tab to see mail here.</Alert>
      ) : null}
      {hasMailbox && !settings?.hasApiKey ? (
        <Alert severity="info">Save an OpenAI API key on the Settings tab so Flexis can classify new mail.</Alert>
      ) : null}
      <Panel>
        <Stack spacing={1.5}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Need action
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Pinned mail in your mailboxes that matches the labels you chose on Settings. This list
              reflects what is in Gmail or Outlook right now, not a time range.
            </Typography>
          </Stack>
          {configured.length > 0 ? (
            <Typography variant="caption" color="text.secondary">
              Watching: {configured.map((slug) => labelName(slug as MailCheckLabelSlug)).join(", ")}
            </Typography>
          ) : null}
          <FilterRow direction="row">
            <Chip
              label={`All (${allItems.length})`}
              color={filter === "all" ? "primary" : "default"}
              onClick={() => setFilter("all")}
            />
            {configured.map((slug) => {
              const count = allItems.filter((item) => item.labelSlug === slug).length;
              return (
                <Chip
                  key={slug}
                  label={`${labelName(slug as MailCheckLabelSlug)} (${count})`}
                  color={filter === slug ? "primary" : "default"}
                  onClick={() => setFilter(slug as MailCheckLabelSlug)}
                />
              );
            })}
          </FilterRow>
        </Stack>
      </Panel>
      {needActionQuery.isPending && hasMailbox ? (
        <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
          <CircularProgress />
        </Box>
      ) : null}
      {hasMailbox && !needActionQuery.isPending && items.length === 0 ? (
        <EmptyState>
          <Typography variant="body2" color="text.secondary">
            No pinned mail needs action for the selected labels. Run Check to classify new messages,
            or adjust Need action labels on Settings.
          </Typography>
        </EmptyState>
      ) : null}
      {items.length > 0 ? <MailCheckInboxTable items={items} /> : null}
    </Stack>
  );
}
