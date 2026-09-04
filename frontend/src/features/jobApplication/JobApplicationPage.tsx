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
import { PipelineBulkProgress } from "@/features/jobApplication/PipelineBulkProgress";
import { usePipelineBulkRun } from "@/features/jobApplication/usePipelineBulkRun";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

type JobApplicationTab = "operations" | "financial" | "resume" | "logs";

export function JobApplicationPage() {
  const [tab, setTab] = useState<JobApplicationTab>("operations");
  const [visited, setVisited] = useState({
    financial: false,
    resume: false,
    logs: false,
  });
  const bulkRun = usePipelineBulkRun();

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

          {bulkRun.session ? <PipelineBulkProgress session={bulkRun.session} /> : null}

          <Tabs
            value={tab}
            onChange={(_event, value: JobApplicationTab) => {
              setTab(value);
              if (value === "financial" || value === "resume" || value === "logs") {
                setVisited((current) => ({ ...current, [value]: true }));
              }
            }}
          >
            <Tab label="Operations" value="operations" />
            <Tab label="Financial" value="financial" />
            <Tab label="Resume generation" value="resume" />
            <Tab label="Logs" value="logs" />
          </Tabs>
          <Box role="tabpanel" hidden={tab !== "operations"}>
            <JobApplicationOperationsTab bulkRun={bulkRun} />
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
        </Stack>
      </Container>
    </Box>
  );
}
