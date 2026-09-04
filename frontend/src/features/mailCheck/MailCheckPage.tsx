import Badge from "@mui/material/Badge";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
import LinearProgress from "@mui/material/LinearProgress";
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
import { MailCheckUsageTab } from "@/features/mailCheck/MailCheckUsageTab";
import { useMailCheckRun } from "@/features/mailCheck/useMailCheckRun";
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

const MailCheckTabs = styled(Tabs)(({ theme }) => ({
  overflow: "visible",
  marginBottom: theme.spacing(0.5),
  "& .MuiTabs-scroller": {
    overflow: "visible !important",
  },
  "& .MuiTabs-flexContainer": {
    overflow: "visible",
  },
}));

const MailCheckTabItem = styled(Tab)(({ theme }) => ({
  overflow: "visible",
  minHeight: 48,
  paddingRight: theme.spacing(3),
  paddingLeft: theme.spacing(2),
}));

const TabLabelRoot = styled("span")(({ theme }) => ({
  display: "inline-flex",
  alignItems: "center",
  position: "relative",
  paddingRight: theme.spacing(2.5),
}));

type MailCheckTab = "need-action" | "inbox" | "check" | "usage";

function TabLabel({ label, count }: { label: string; count: number }) {
  if (count <= 0) {
    return <>{label}</>;
  }

  return (
    <TabLabelRoot>
      {label}
      <Badge
        color="error"
        badgeContent={count}
        sx={{
          position: "absolute",
          top: -6,
          right: 0,
          "& .MuiBadge-badge": {
            position: "static",
            transform: "none",
          },
        }}
      />
    </TabLabelRoot>
  );
}

export function MailCheckPage() {
  const [tab, setTab] = useState<MailCheckTab>("need-action");
  const [visited, setVisited] = useState({
    inbox: false,
    check: false,
    usage: false,
  });
  const settingsQuery = useQuery({
    queryKey: mailCheckSettingsQueryKey,
    queryFn: getMailCheckSettings,
  });
  const settings = settingsQuery.data;
  const mailboxes = settings?.mailboxes ?? [];
  const hasMailbox = mailboxes.length > 0;
  const mailCheckRun = useMailCheckRun(mailboxes);
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
              Connect Gmail or Outlook and add your OpenAI key on Settings, then Flexis classifies
              mail, pins what matters, and trashes noise.
            </Typography>
          </Stack>

          {mailCheckRun.checking ? (
            <Box
              sx={{
                border: 1,
                borderColor: "primary.main",
                borderRadius: 1,
                px: 2,
                py: 1.5,
                bgcolor: "action.hover",
              }}
            >
              <Stack spacing={1}>
                <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
                  <CircularProgress size={18} />
                  <Stack spacing={0.25}>
                    <Typography variant="body2">
                      {mailCheckRun.session?.message ?? "Manual check running…"}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {mailCheckRun.session?.stageMessage ??
                        "Open the Check tab for stages, elapsed time, and server timing."}
                    </Typography>
                  </Stack>
                </Stack>
                <LinearProgress />
              </Stack>
            </Box>
          ) : null}

          <MailCheckTabs
            value={tab}
            onChange={(_event, value: MailCheckTab) => {
              setTab(value);
              if (value === "inbox" || value === "check" || value === "usage") {
                setVisited((current) => ({ ...current, [value]: true }));
              }
            }}
          >
            <MailCheckTabItem
              label={<TabLabel label="Need action" count={needActionCount} />}
              value="need-action"
            />
            <MailCheckTabItem label="Inbox" value="inbox" />
            <MailCheckTabItem label="Check" value="check" />
            <MailCheckTabItem label="Usage" value="usage" />
          </MailCheckTabs>
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
              <MailCheckCheckTab settings={settings} mailCheckRun={mailCheckRun} />
            </Box>
          ) : null}
          {tab === "usage" || visited.usage ? (
            <Box role="tabpanel" hidden={tab !== "usage"}>
              <MailCheckUsageTab />
            </Box>
          ) : null}
        </Stack>
      </Container>
    </Box>
  );
}
