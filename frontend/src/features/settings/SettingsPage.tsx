import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useAuth } from "@/shared/auth/AuthProvider";
import { UsersManagement } from "./UsersManagement";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

export function SettingsPage() {
  const auth = useAuth();
  const isAdmin = auth.user?.role === "Admin";

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="lg">
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Typography variant="overline" color="secondary">
              Account
            </Typography>
            <Typography variant="h4" component="h1">
              Settings
            </Typography>
            <AccentRule />
            {!isAdmin ? (
              <Typography variant="body2" color="text.secondary">
                Job Application settings, including Gmail, profiles, and sources, live under Job
                Application. User management is available to admins.
              </Typography>
            ) : null}
          </Stack>
          {isAdmin ? <UsersManagement /> : null}
        </Stack>
      </Container>
    </Box>
  );
}
