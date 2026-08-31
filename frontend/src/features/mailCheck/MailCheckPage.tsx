import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useState } from "react";
import { MailCheckCheckTab } from "@/features/mailCheck/MailCheckCheckTab";
import { MailCheckInboxTab } from "@/features/mailCheck/MailCheckInboxTab";
import { MailCheckSettingsTab } from "@/features/mailCheck/MailCheckSettingsTab";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

type MailCheckTab = "inbox" | "check" | "settings";

export function MailCheckPage() {
  const [tab, setTab] = useState<MailCheckTab>("inbox");
  const [visited, setVisited] = useState({
    check: false,
    settings: false,
  });

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
              Connect Gmail or Outlook on Settings, add your OpenAI key, and Flexis labels interview
              mail, pins keepers, and trashes application noise.
            </Typography>
          </Stack>
          <Tabs
            value={tab}
            onChange={(_event, value: MailCheckTab) => {
              setTab(value);
              if (value === "check" || value === "settings") {
                setVisited((current) => ({ ...current, [value]: true }));
              }
            }}
          >
            <Tab label="Inbox" value="inbox" />
            <Tab label="Check" value="check" />
            <Tab label="Settings" value="settings" />
          </Tabs>
          <Box role="tabpanel" hidden={tab !== "inbox"}>
            <MailCheckInboxTab />
          </Box>
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
