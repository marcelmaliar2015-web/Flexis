import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useState } from "react";
import { HelpFinancialTab } from "@/features/help/HelpFinancialTab";
import { HelpGoogleTab } from "@/features/help/HelpGoogleTab";
import { HelpLogsTab } from "@/features/help/HelpLogsTab";
import { HelpOperationsTab } from "@/features/help/HelpOperationsTab";
import { HelpOverviewTab } from "@/features/help/HelpOverviewTab";
import { HelpProblemsTab } from "@/features/help/HelpProblemsTab";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

type HelpTab = "overview" | "google" | "operations" | "financial" | "logs" | "problems";

export function HelpPage() {
  const [tab, setTab] = useState<HelpTab>("overview");
  const [visited, setVisited] = useState({
    google: false,
    operations: false,
    financial: false,
    logs: false,
    problems: false,
  });

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="lg">
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Typography variant="overline" color="secondary">
              Help
            </Typography>
            <Typography variant="h4" component="h1">
              Guides
            </Typography>
            <AccentRule />
            <Typography variant="body2" color="text.secondary">
              Structured guides for the workspace, Google connect, Operations, Financial, Logs, and
              common problems.
            </Typography>
          </Stack>
          <Tabs
            value={tab}
            onChange={(_event, value: HelpTab) => {
              setTab(value);
              if (value !== "overview") {
                setVisited((current) => ({ ...current, [value]: true }));
              }
            }}
            variant="scrollable"
            allowScrollButtonsMobile
          >
            <Tab label="Overview" value="overview" />
            <Tab label="Google setup" value="google" />
            <Tab label="Operations" value="operations" />
            <Tab label="Financial" value="financial" />
            <Tab label="Logs" value="logs" />
            <Tab label="Problems" value="problems" />
          </Tabs>
          <Box role="tabpanel" hidden={tab !== "overview"}>
            <HelpOverviewTab />
          </Box>
          {tab === "google" || visited.google ? (
            <Box role="tabpanel" hidden={tab !== "google"}>
              <HelpGoogleTab />
            </Box>
          ) : null}
          {tab === "operations" || visited.operations ? (
            <Box role="tabpanel" hidden={tab !== "operations"}>
              <HelpOperationsTab />
            </Box>
          ) : null}
          {tab === "financial" || visited.financial ? (
            <Box role="tabpanel" hidden={tab !== "financial"}>
              <HelpFinancialTab />
            </Box>
          ) : null}
          {tab === "logs" || visited.logs ? (
            <Box role="tabpanel" hidden={tab !== "logs"}>
              <HelpLogsTab />
            </Box>
          ) : null}
          {tab === "problems" || visited.problems ? (
            <Box role="tabpanel" hidden={tab !== "problems"}>
              <HelpProblemsTab />
            </Box>
          ) : null}
        </Stack>
      </Container>
    </Box>
  );
}
