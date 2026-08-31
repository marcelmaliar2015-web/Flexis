import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Container from "@mui/material/Container";
import Divider from "@mui/material/Divider";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { alpha, styled } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { getHealthStatus, healthQueryKey } from "@/shared/api/health";
import { appPaths } from "@/shared/config/paths";

const capabilities = [
  {
    index: "01",
    title: "Relational core",
    body: "PostgreSQL holds structured records with the discipline of a system of record.",
  },
  {
    index: "02",
    title: "Document store",
    body: "MongoDB keeps flexible collections close to the same operating surface.",
  },
  {
    index: "03",
    title: "Continuity",
    body: "Live health makes the stack visible before work begins, not after it fails.",
  },
] as const;

const HeroBand = styled(Box)(({ theme }) => ({
  position: "relative",
  overflow: "hidden",
  paddingBlock: theme.spacing(8),
  background: [
    `radial-gradient(1000px 480px at 0% -20%, ${alpha(theme.palette.secondary.light, 0.28)}, transparent 58%)`,
    `radial-gradient(820px 420px at 100% 0%, ${alpha(theme.palette.primary.light, 0.16)}, transparent 52%)`,
    theme.palette.background.default,
  ].join(", "),
  [theme.breakpoints.up("md")]: {
    paddingBlock: theme.spacing(13),
  },
}));

const PreviewPanel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3.5),
  boxShadow: "0 28px 64px rgba(14, 39, 68, 0.1)",
}));

const CapabilityCard = styled(Box)(({ theme }) => ({
  height: "100%",
  padding: theme.spacing(3.5),
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
}));

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

const CapabilitiesBand = styled(Box)(({ theme }) => ({
  paddingBlock: theme.spacing(7),
  borderTop: `1px solid ${theme.palette.divider}`,
  [theme.breakpoints.up("md")]: {
    paddingBlock: theme.spacing(10),
  },
}));

function platformChipColor(status: string | undefined) {
  if (status === "Healthy") {
    return "success" as const;
  }
  if (status) {
    return "warning" as const;
  }
  return "default" as const;
}

function platformChipLabel(status: string | undefined, failed: boolean) {
  if (failed) {
    return "Unreachable";
  }
  return status ?? "Checking";
}

export function HomePage() {
  const healthQuery = useQuery({
    queryKey: healthQueryKey,
    queryFn: getHealthStatus,
  });

  const platformStatus = healthQuery.data?.status;

  return (
    <Stack>
      <HeroBand>
        <Container maxWidth="lg">
          <Grid container spacing={{ xs: 5, md: 8 }} sx={{ alignItems: "center" }}>
            <Grid size={{ xs: 12, md: 7 }}>
              <Stack spacing={2.5} sx={{ alignItems: "flex-start" }}>
                <Typography variant="overline" color="secondary">
                  Enterprise platform
                </Typography>
                <Typography variant="h1" component="h1">
                  Composed operations. Quiet confidence.
                </Typography>
                <AccentRule />
                <Box sx={{ maxWidth: "38rem" }}>
                  <Typography variant="body1" color="text.secondary">
                    Flexis is a precise workspace for teams that run on durable data.
                    Relational records, document collections, and system integrity share
                    one calm surface.
                  </Typography>
                </Box>
                <Button component={RouterLink} to={appPaths.signIn} size="large">
                  Sign in
                </Button>
              </Stack>
            </Grid>
            <Grid size={{ xs: 12, md: 5 }}>
              <PreviewPanel>
                <Stack spacing={2.5}>
                  <Stack
                    direction="row"
                    spacing={2}
                    sx={{ alignItems: "center", justifyContent: "space-between" }}
                  >
                    <Typography variant="subtitle2">Live platform</Typography>
                    <Chip
                      size="small"
                      color={platformChipColor(platformStatus)}
                      label={platformChipLabel(platformStatus, healthQuery.isError)}
                    />
                  </Stack>
                  <Divider />
                  <PreviewRow label="PostgreSQL" detail="System of record" />
                  <PreviewRow label="MongoDB" detail="Document collections" />
                  <PreviewRow label="Health" detail="Continuity of service" />
                </Stack>
              </PreviewPanel>
            </Grid>
          </Grid>
        </Container>
      </HeroBand>
      <CapabilitiesBand>
        <Container maxWidth="lg">
          <Stack spacing={4}>
            <Box sx={{ maxWidth: "36rem" }}>
              <Stack spacing={1}>
                <Typography variant="overline" color="secondary">
                  Designed to last
                </Typography>
                <Typography variant="h2" component="h2">
                  Structure where it matters. Flexibility where it counts.
                </Typography>
              </Stack>
            </Box>
            <Grid container spacing={2.5}>
              {capabilities.map((capability) => (
                <Grid key={capability.title} size={{ xs: 12, md: 4 }}>
                  <CapabilityCard>
                    <Stack spacing={1.5}>
                      <Typography variant="overline" color="secondary">
                        {capability.index}
                      </Typography>
                      <Typography variant="h6" component="h3">
                        {capability.title}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {capability.body}
                      </Typography>
                    </Stack>
                  </CapabilityCard>
                </Grid>
              ))}
            </Grid>
          </Stack>
        </Container>
      </CapabilitiesBand>
    </Stack>
  );
}

function PreviewRow({ label, detail }: { label: string; detail: string }) {
  return (
    <Stack
      direction="row"
      spacing={2}
      sx={{ justifyContent: "space-between", alignItems: "baseline" }}
    >
      <Typography variant="body2">{label}</Typography>
      <Typography variant="body2" color="text.secondary">
        {detail}
      </Typography>
    </Stack>
  );
}
