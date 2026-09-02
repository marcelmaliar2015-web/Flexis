import Badge from "@mui/material/Badge";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { MailCheckCheckTab } from "@/features/mailCheck/MailCheckCheckTab";
import { MailCheckInboxTab } from "@/features/mailCheck/MailCheckInboxTab";
import { MailCheckNeedActionTab } from "@/features/mailCheck/MailCheckNeedActionTab";
import { MailCheckSettingsTab } from "@/features/mailCheck/MailCheckSettingsTab";
import {
  getMailCheckNeedAction,
  getMailCheckSettings,
  mailCheckNeedActionQueryKey,
  mailCheckSettingsQueryKey,
} from "@/shared/api/mailCheck";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

type MailCheckTab = "need-action" | "inbox" | "check" | "settings";

function TabLabel({ label, count }: { label: string; count: number }) {
  if (count <= 0) {
    return <>{label}</>;
  }

  return (
    <Badge
      color="error"
      badgeContent={count}
      sx={{ "& .MuiBadge-badge": { right: -12, top: 2 } }}
    >
      <span>{label}</span>
    </Badge>
  );
}

export function MailCheckPage() {
  const [tab, setTab] = useState<MailCheckTab>("need-action");
  const [visited, setVisited] = useState({
    inbox: false,
    check: false,
    settings: false,
  });
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
  const needActionCount = needActionQuery.data?.items.length ?? 0;

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="xl">
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Typography variant="overline" color="secondary">
              Mail Check
            </Typography>
            <Typography variant="h4" component="h1">
              Inbox triage
            </Typography>
            <AccentRule />
            <Typography variant="body2" color="text.secondary">
              Connect Gmail or Outlook on Settings, add your OpenAI key, and Flexis classifies mail,
              pins what matters, and trashes noise.
            </Typography>
          </Stack>
          <Tabs
            value={tab}
            onChange={(_event, value: MailCheckTab) => {
              setTab(value);
              if (value === "inbox" || value === "check" || value === "settings") {
                setVisited((current) => ({ ...current, [value]: true }));
              }
            }}
          >
            <Tab
              label={<TabLabel label="Need action" count={needActionCount} />}
              value="need-action"
            />
            <Tab label="Inbox" value="inbox" />
            <Tab label="Check" value="check" />
            <Tab label="Settings" value="settings" />
          </Tabs>
          <Box role="tabpanel" hidden={tab !== "need-action"}>
            <MailCheckNeedActionTab />
          </Box>
          {tab === "inbox" || visited.inbox ? (
            <Box role="tabpanel" hidden={tab !== "inbox"}>
              <MailCheckInboxTab />
            </Box>
          ) : null}
          {tab === "check" || visited.check ? (
            <Box role="tabpanel" hidden={tab !== "check"}>
              <MailCheckCheckTab />
            </Box>
          ) : null}
          {tab === "settings" || visited.settings ? (
            <Box role="tabpanel" hidden={tab !== "settings"}>
              <MailCheckSettingsTab />
            </Box>
          ) : null}
        </Stack>
      </Container>
    </Box>
  );
}
