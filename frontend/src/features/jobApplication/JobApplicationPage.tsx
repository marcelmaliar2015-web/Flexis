import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useState } from "react";
import { JobApplicationFinancialTab } from "@/features/jobApplication/JobApplicationFinancialTab";
import { JobApplicationOperationsTab } from "@/features/jobApplication/JobApplicationOperationsTab";
import { JobApplicationProfilesTab } from "@/features/jobApplication/JobApplicationProfilesTab";
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

type JobApplicationTab = "operations" | "profiles" | "financial" | "resume";

export function JobApplicationPage() {
  const [tab, setTab] = useState<JobApplicationTab>("operations");
  const [visited, setVisited] = useState({
    profiles: false,
    financial: false,
    resume: false,
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
              if (value === "profiles" || value === "financial" || value === "resume") {
                setVisited((current) => ({ ...current, [value]: true }));
              }
            }}
          >
            <Tab label="Operations" value="operations" />
            <Tab label="Profiles" value="profiles" />
            <Tab label="Financial" value="financial" />
            <Tab label="Resume generation" value="resume" />
          </Tabs>
          <Box role="tabpanel" hidden={tab !== "operations"}>
            <JobApplicationOperationsTab bulkRun={bulkRun} />
          </Box>
          {tab === "profiles" || visited.profiles ? (
            <Box role="tabpanel" hidden={tab !== "profiles"}>
              <JobApplicationProfilesTab />
            </Box>
          ) : null}
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
        </Stack>
      </Container>
    </Box>
  );
}
