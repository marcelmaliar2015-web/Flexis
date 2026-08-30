import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { Link as RouterLink } from "react-router-dom";

export function HomePage() {
  return (
    <Stack spacing={2}>
      <Typography variant="h4" component="h1">
        Flexis
      </Typography>
      <Typography>
        Frontend and API are connected. Use Health to confirm PostgreSQL and MongoDB.
      </Typography>
      <Button component={RouterLink} to="/health">
        View health
      </Button>
    </Stack>
  );
}
