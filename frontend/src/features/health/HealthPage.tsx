import Alert from "@mui/material/Alert";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useHealthStatus } from "@/features/health/useHealthStatus";

export function HealthPage() {
  const healthQuery = useHealthStatus();

  if (healthQuery.isPending) {
    return <CircularProgress />;
  }

  if (healthQuery.isError || !healthQuery.data) {
    const message =
      healthQuery.error instanceof Error
        ? healthQuery.error.message
        : "API is not running. Start backend/src/Flexis.Api.";
    return <Alert severity="error">{message}</Alert>;
  }

  return (
    <Stack spacing={2}>
      <Typography variant="h4" component="h1">
        Health
      </Typography>
      <Chip label={healthQuery.data.status} color={healthQuery.data.status === "Healthy" ? "success" : "warning"} />
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
}
