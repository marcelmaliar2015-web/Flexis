import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { DashboardBoard } from "@/features/dashboard/DashboardBoard";
import { AccentRule } from "@/features/dashboard/dashboardUi";
import { useDashboardData } from "@/features/dashboard/useDashboardData";

export function DashboardPage() {
  const data = useDashboardData();

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="xl">
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Typography variant="overline" color="secondary">
              Dashboard
            </Typography>
            <Typography variant="h4" component="h1">
              Workspace
            </Typography>
            <AccentRule />
            <Typography variant="body2" color="text.secondary">
              {data.user
                ? `${data.user.displayName} · ${data.user.role}. Live status for this account. Listing counts and price come from profile main tabs after Gmail can read those sheets.`
                : "Live status for this account."}
            </Typography>
          </Stack>
          <DashboardBoard
            isAdmin={data.isAdmin}
            health={data.healthQuery.data}
            healthError={data.healthQuery.error}
            google={data.googleQuery.data}
            googleError={data.googleQuery.error}
            pipeline={data.pipelineQuery.data}
            pipelineError={data.pipelineQuery.error}
            financial={data.financialQuery.data}
            financialError={data.financialQuery.error}
            logs={data.logsQuery.data}
            logsError={data.logsQuery.error}
            users={data.usersQuery.data}
            usersError={data.usersQuery.error}
          />
        </Stack>
      </Container>
    </Box>
  );
}
