import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useState } from "react";
import { JobApplicationFinancialTab } from "@/features/jobApplication/JobApplicationFinancialTab";
import { JobApplicationLogsTab } from "@/features/jobApplication/JobApplicationLogsTab";
import { JobApplicationOperationsTab } from "@/features/jobApplication/JobApplicationOperationsTab";
import { JobApplicationResumeTab } from "@/features/jobApplication/JobApplicationResumeTab";
import { JobApplicationSettingsTab } from "@/features/jobApplication/JobApplicationSettingsTab";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

type JobApplicationTab = "operations" | "financial" | "resume" | "logs" | "settings";

export function JobApplicationPage() {
  const [tab, setTab] = useState<JobApplicationTab>("operations");
  const [visited, setVisited] = useState({
    financial: false,
    resume: false,
    logs: false,
    settings: false,
  });

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="xl">
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Typography variant="overline" color="secondary">
              Job Application
            </Typography>
            <Typography variant="h4" component="h1">
              Workspace
            </Typography>
            <AccentRule />
          </Stack>
          <Tabs
            value={tab}
            onChange={(_event, value: JobApplicationTab) => {
              setTab(value);
              if (value === "financial" || value === "resume" || value === "logs" || value === "settings") {
                setVisited((current) => ({ ...current, [value]: true }));
              }
            }}
          >
            <Tab label="Operations" value="operations" />
            <Tab label="Financial" value="financial" />
            <Tab label="Resume generation" value="resume" />
            <Tab label="Logs" value="logs" />
            <Tab label="Settings" value="settings" />
          </Tabs>
          <Box role="tabpanel" hidden={tab !== "operations"}>
            <JobApplicationOperationsTab />
          </Box>
          {tab === "financial" || visited.financial ? (
            <Box role="tabpanel" hidden={tab !== "financial"}>
              <JobApplicationFinancialTab />
            </Box>
          ) : null}
          {tab === "resume" || visited.resume ? (
            <Box role="tabpanel" hidden={tab !== "resume"}>
              <JobApplicationResumeTab />
            </Box>
          ) : null}
          {tab === "logs" || visited.logs ? (
            <Box role="tabpanel" hidden={tab !== "logs"}>
              <JobApplicationLogsTab />
            </Box>
          ) : null}
          {tab === "settings" || visited.settings ? (
            <Box role="tabpanel" hidden={tab !== "settings"}>
              <JobApplicationSettingsTab />
            </Box>
          ) : null}
        </Stack>
      </Container>
    </Box>
  );
}
