import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { JobApplicationSettingsTab } from "@/features/jobApplication/JobApplicationSettingsTab";
import { MailCheckSettingsTab } from "@/features/mailCheck/MailCheckSettingsTab";
import { AccountProfile } from "@/features/settings/AccountProfile";
import { GoogleClientSettings } from "@/features/settings/GoogleClientSettings";
import { MicrosoftClientSettings } from "@/features/settings/MicrosoftClientSettings";
import { UsersManagement } from "@/features/settings/UsersManagement";
import { useAuth } from "@/shared/auth/AuthProvider";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

export type SettingsTabId = "account" | "job-application" | "mail-check" | "admin";

const settingsTabIds: SettingsTabId[] = ["account", "job-application", "mail-check", "admin"];

function parseSettingsTab(raw: string | null, isAdmin: boolean): SettingsTabId {
  if (raw && settingsTabIds.includes(raw as SettingsTabId)) {
    const tab = raw as SettingsTabId;
    if (tab === "admin" && !isAdmin) {
      return "account";
    }

    return tab;
  }

  return "account";
}

export function SettingsPage() {
  const auth = useAuth();
  const isAdmin = auth.user?.role === "Admin";
  const [searchParams, setSearchParams] = useSearchParams();
  const tabFromUrl = useMemo(
    () => parseSettingsTab(searchParams.get("tab"), isAdmin),
    [searchParams, isAdmin],
  );
  const [tab, setTab] = useState<SettingsTabId>(tabFromUrl);
  const [visited, setVisited] = useState<Record<SettingsTabId, boolean>>({
    account: true,
    "job-application": false,
    "mail-check": false,
    admin: false,
  });

  useEffect(() => {
    setTab(tabFromUrl);
    setVisited((current) => ({ ...current, [tabFromUrl]: true }));
  }, [tabFromUrl]);

  useEffect(() => {
    if (searchParams.get("google")) {
      setTab("job-application");
      setVisited((current) => ({ ...current, "job-application": true }));
      return;
    }

    if (searchParams.get("mailbox")) {
      setTab("mail-check");
      setVisited((current) => ({ ...current, "mail-check": true }));
    }
  }, [searchParams]);

  function selectTab(next: SettingsTabId) {
    setTab(next);
    setVisited((current) => ({ ...current, [next]: true }));
    const params = new URLSearchParams(searchParams);
    if (next === "account") {
      params.delete("tab");
    } else {
      params.set("tab", next);
    }
    setSearchParams(params, { replace: true });
  }

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="xl">
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Typography variant="overline" color="secondary">
              Account
            </Typography>
            <Typography variant="h4" component="h1">
              Settings
            </Typography>
            <AccentRule />
            <Typography variant="body2" color="text.secondary">
              Account, Job Application, and Mail Check settings live here
              {isAdmin ? ", with Admin for Google Cloud, Microsoft, and users" : ""}.
            </Typography>
          </Stack>

          <Tabs
            value={tab}
            onChange={(_event, value: SettingsTabId) => {
              selectTab(value);
            }}
            variant="scrollable"
            allowScrollButtonsMobile
          >
            <Tab label="Account" value="account" />
            <Tab label="Job Application" value="job-application" />
            <Tab label="Mail Check" value="mail-check" />
            {isAdmin ? <Tab label="Admin" value="admin" /> : null}
          </Tabs>

          <Box role="tabpanel" hidden={tab !== "account"}>
            <AccountProfile />
          </Box>
          {tab === "job-application" || visited["job-application"] ? (
            <Box role="tabpanel" hidden={tab !== "job-application"}>
              <JobApplicationSettingsTab />
            </Box>
          ) : null}
          {tab === "mail-check" || visited["mail-check"] ? (
            <Box role="tabpanel" hidden={tab !== "mail-check"}>
              <MailCheckSettingsTab />
            </Box>
          ) : null}
          {isAdmin && (tab === "admin" || visited.admin) ? (
            <Box role="tabpanel" hidden={tab !== "admin"}>
              <Stack spacing={4}>
                <GoogleClientSettings />
                <MicrosoftClientSettings />
                <UsersManagement />
              </Stack>
            </Box>
          ) : null}
        </Stack>
      </Container>
    </Box>
  );
}
