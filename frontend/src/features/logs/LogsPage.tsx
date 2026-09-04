import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { JobApplicationLogsTab } from "@/features/jobApplication/JobApplicationLogsTab";
import { MailCheckLogTab } from "@/features/mailCheck/MailCheckLogTab";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

export type LogsTabId = "job-application" | "mail-check";

const logsTabIds: LogsTabId[] = ["job-application", "mail-check"];

function parseLogsTab(raw: string | null): LogsTabId {
  if (raw && logsTabIds.includes(raw as LogsTabId)) {
    return raw as LogsTabId;
  }

  return "job-application";
}

export function LogsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const tabFromUrl = useMemo(() => parseLogsTab(searchParams.get("tab")), [searchParams]);
  const [tab, setTab] = useState<LogsTabId>(tabFromUrl);
  const [visited, setVisited] = useState<Record<LogsTabId, boolean>>({
    "job-application": true,
    "mail-check": false,
  });

  useEffect(() => {
    setTab(tabFromUrl);
    setVisited((current) => ({ ...current, [tabFromUrl]: true }));
  }, [tabFromUrl]);

  function selectTab(next: LogsTabId) {
    setTab(next);
    setVisited((current) => ({ ...current, [next]: true }));
    const params = new URLSearchParams(searchParams);
    params.set("tab", next);
    setSearchParams(params, { replace: true });
  }

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="xl">
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Typography variant="overline" color="secondary">
              Logs
            </Typography>
            <Typography variant="h4" component="h1">
              Activity
            </Typography>
            <AccentRule />
            <Typography variant="body2" color="text.secondary">
              Job Application and Mail Check activity in one place. Each tab is paged so long
              histories stay readable.
            </Typography>
          </Stack>

          <Tabs value={tab} onChange={(_event, value: LogsTabId) => selectTab(value)}>
            <Tab label="Job Application" value="job-application" />
            <Tab label="Mail Check" value="mail-check" />
          </Tabs>
          <Box role="tabpanel" hidden={tab !== "job-application"}>
            <JobApplicationLogsTab />
          </Box>
          {tab === "mail-check" || visited["mail-check"] ? (
            <Box role="tabpanel" hidden={tab !== "mail-check"}>
              <MailCheckLogTab />
            </Box>
          ) : null}
        </Stack>
      </Container>
    </Box>
  );
}
