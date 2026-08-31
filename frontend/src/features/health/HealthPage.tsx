import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type { ReactNode } from "react";
import { useHealthStatus } from "@/features/health/useHealthStatus";

export function HealthPage() {
  const healthQuery = useHealthStatus();

  let body: ReactNode = <CircularProgress />;

  if (healthQuery.isPending) {
    body = <CircularProgress />;
  } else if (healthQuery.data) {
    body = (
      <Stack spacing={2}>
        <Typography variant="h4" component="h1">
          Health
        </Typography>
        <Box>
          <Chip
            label={healthQuery.data.status}
            color={healthQuery.data.status === "Healthy" ? "success" : "warning"}
          />
        </Box>
        {healthQuery.data.checks.map((check) => (
          <Alert
            key={check.name}
            severity={check.status === "Healthy" ? "success" : "warning"}
          >
            {check.name}: {check.status}
            {check.description ? ` — ${check.description}` : ""}
          </Alert>
        ))}
      </Stack>
    );
  } else {
    body = null;
  }

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="md">{body}</Container>
    </Box>
  );
}
