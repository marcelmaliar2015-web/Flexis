import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useState } from "react";
import { JobApplicationSettingsTab } from "@/features/jobApplication/JobApplicationSettingsTab";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

type JobApplicationTab = "settings";

export function JobApplicationPage() {
  const [tab, setTab] = useState<JobApplicationTab>("settings");

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="lg">
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
            onChange={(_event, value: JobApplicationTab) => setTab(value)}
          >
            <Tab label="Settings" value="settings" />
          </Tabs>
          {tab === "settings" ? <JobApplicationSettingsTab /> : null}
        </Stack>
      </Container>
    </Box>
  );
}
