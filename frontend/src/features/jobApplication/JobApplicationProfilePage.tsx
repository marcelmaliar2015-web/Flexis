import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import {
  formatPrice,
  formatRate,
  parseRate,
} from "@/features/jobApplication/financialUi";
import { ProfileInfoPanel } from "@/features/jobApplication/ProfileInfoPanel";
import { refreshJobApplicationWorkspace } from "@/features/jobApplication/refreshWorkspace";
import { getJobFinancialBoard, jobFinancialQueryKey, updateJobFinancialRates } from "@/shared/api/financial";
import { getGoogleConnection, googleConnectionQueryKey } from "@/shared/api/google";
import {
  listProfileBannedCompanies,
  profileBannedQueryKey,
} from "@/shared/api/jobCatalog";
import { getJobPipelineBoard, jobPipelineQueryKey } from "@/shared/api/pipeline";
import { isQueryLoading } from "@/shared/api/queryState";
import { appPaths } from "@/shared/config/paths";
import { userFacingError } from "@/shared/api/errors";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3),
}));

const RateField = styled(TextField)({
  width: 120,
});

export function JobApplicationProfilePage() {
  const { entryId } = useParams<{ entryId: string }>();
  const queryClient = useQueryClient();
  const connectionQuery = useQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });
  const connected = connectionQuery.data?.connected === true;
  const financialQuery = useQuery({
    queryKey: jobFinancialQueryKey,
    queryFn: getJobFinancialBoard,
    enabled: Boolean(entryId),
  });
  const pipelineQuery = useQuery({
    queryKey: jobPipelineQueryKey,
    queryFn: getJobPipelineBoard,
    enabled: Boolean(entryId),
  });
  const row = financialQuery.data?.rows.find((item) => item.entryId === entryId);
  const entry = pipelineQuery.data?.entries.find((item) => item.id === entryId);
  const profileId = row?.profileId ?? entry?.profileId ?? "";
  const bannedQuery = useQuery({
    queryKey: profileBannedQueryKey(profileId),
    queryFn: () => listProfileBannedCompanies(profileId),
    enabled: Boolean(profileId),
  });
  const [applyRate, setApplyRate] = useState("");
  const [bonusRate, setBonusRate] = useState("");
  const [rateError, setRateError] = useState<string | null>(null);

  useEffect(() => {
    if (!row) {
      return;
    }

    setApplyRate(formatRate(row.applyRate));
    setBonusRate(formatRate(row.bonusRate));
    setRateError(null);
  }, [row]);

  const ratesMutation = useMutation({
    mutationFn: ({ applyRate: nextApply, bonusRate: nextBonus }: { applyRate: number; bonusRate: number }) =>
      updateJobFinancialRates(entryId ?? "", {
        applyRate: nextApply,
        bonusRate: nextBonus,
      }),
    onSuccess: async () => {
      setRateError(null);
      await refreshJobApplicationWorkspace(queryClient);
    },
    onError: (error) => {
      setRateError(userFacingError(error) ?? "Could not save rates.");
    },
  });

  function saveRates() {
    const nextApply = parseRate(applyRate);
    const nextBonus = parseRate(bonusRate);
    if (nextApply === null || nextBonus === null) {
      setRateError("Rates must be numbers from 0 to 10000.");
      return;
    }

    ratesMutation.mutate({ applyRate: nextApply, bonusRate: nextBonus });
  }

  const loading = isQueryLoading(financialQuery.data, financialQuery.isPending)
    || isQueryLoading(pipelineQuery.data, pipelineQuery.isPending);

  if (!entryId) {
    return null;
  }

  if (!loading && !row) {
    return (
      <Box sx={{ py: { xs: 4, md: 6 } }}>
        <Container maxWidth="xl">
          <Stack spacing={2}>
            <Button component={RouterLink} to={appPaths.jobApplication} variant="text">
              Back to Job Application
            </Button>
            <Alert severity="warning">This profile row was not found. It may have been deleted.</Alert>
          </Stack>
        </Container>
      </Box>
    );
  }

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="xl">
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Button
              component={RouterLink}
              to={appPaths.jobApplication}
              variant="text"
              sx={{ alignSelf: "flex-start" }}
            >
              Back to Job Application
            </Button>
            <Typography variant="overline" color="secondary">
              Profile
            </Typography>
            <Typography variant="h4" component="h1">
              {row?.profileTitle ?? "Profile"}
            </Typography>
            <AccentRule />
            <Typography variant="body2" color="text.secondary">
              {row?.sourceLabel ?? "Loading source…"}
            </Typography>
          </Stack>

          <Panel>
            <Stack spacing={2}>
              <Typography variant="h6" component="h2">
                Sheet and pairing
              </Typography>
              <Stack spacing={1}>
                <Typography variant="subtitle2">Source</Typography>
                <Typography variant="body2">{row?.sourceLabel ?? "—"}</Typography>
              </Stack>
              <Stack spacing={1}>
                <Typography variant="subtitle2">Profile sheet</Typography>
                {row?.profileUrl ? (
                  <Link href={row.profileUrl} target="_blank" rel="noopener noreferrer">
                    {row.profileUrl}
                  </Link>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    No sheet URL yet. Connect Gmail and create the profile on Settings.
                  </Typography>
                )}
              </Stack>
              <Button
                component={RouterLink}
                to={appPaths.jobApplicationPipeline(entryId)}
                variant="outlined"
                sx={{ alignSelf: "flex-start" }}
              >
                Open Operations for this row
              </Button>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={2}>
              <Typography variant="h6" component="h2">
                Rates
              </Typography>
              <Stack direction={{ xs: "column", sm: "row" }} spacing={2} useFlexGap>
                <RateField
                  size="small"
                  label="Apply rate"
                  value={applyRate}
                  onChange={(event) => setApplyRate(event.target.value)}
                  disabled={!row || ratesMutation.isPending}
                />
                <RateField
                  size="small"
                  label="Bonus rate"
                  value={bonusRate}
                  onChange={(event) => setBonusRate(event.target.value)}
                  disabled={!row || ratesMutation.isPending}
                />
                <Button
                  variant="contained"
                  onClick={saveRates}
                  disabled={!row || ratesMutation.isPending}
                >
                  Save rates
                </Button>
              </Stack>
              {rateError ? <Alert severity="error">{rateError}</Alert> : null}
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={2}>
              <Typography variant="h6" component="h2">
                Apply status
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Counts from the Status column on this profile sheet. Applied and Interview drive
                price with the rates above.
              </Typography>
              {row ? (
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell align="left">Period</TableCell>
                        <TableCell align="left">Applied</TableCell>
                        <TableCell align="left">Interviews</TableCell>
                        <TableCell align="left">Listings</TableCell>
                        <TableCell align="left">Price</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      <TableRow>
                        <TableCell align="left">Today</TableCell>
                        <TableCell align="left">{row.applied}</TableCell>
                        <TableCell align="left">{row.interviews}</TableCell>
                        <TableCell align="left">{row.total}</TableCell>
                        <TableCell align="left">{formatPrice(row.price)}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell align="left">Archived</TableCell>
                        <TableCell align="left">{row.archivedApplied}</TableCell>
                        <TableCell align="left">{row.archivedInterviews}</TableCell>
                        <TableCell align="left">{row.archivedTotal}</TableCell>
                        <TableCell align="left">{formatPrice(row.archivedPrice)}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell align="left">Lifetime</TableCell>
                        <TableCell align="left">{row.lifetimeApplied}</TableCell>
                        <TableCell align="left">{row.lifetimeInterviews}</TableCell>
                        <TableCell align="left">{row.lifetimeTotal}</TableCell>
                        <TableCell align="left">{formatPrice(row.lifetimePrice)}</TableCell>
                      </TableRow>
                    </TableBody>
                  </Table>
                </TableContainer>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  Loading apply status…
                </Typography>
              )}
            </Stack>
          </Panel>

          <ProfileInfoPanel actionsEnabled={connected} profileId={profileId || undefined} />

          <Panel>
            <Stack spacing={2}>
              <Stack
                direction="row"
                spacing={2}
                sx={{ alignItems: "center", justifyContent: "space-between" }}
              >
                <Typography variant="h6" component="h2">
                  Banned companies
                </Typography>
                <Button
                  component={RouterLink}
                  to={appPaths.jobApplicationPipeline(entryId)}
                  variant="text"
                >
                  Manage on Operations
                </Button>
              </Stack>
              {(bannedQuery.data?.length ?? 0) === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  No banned companies for this profile.
                </Typography>
              ) : (
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell align="left">Company</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {(bannedQuery.data ?? []).map((item) => (
                        <TableRow key={item.id}>
                          <TableCell align="left">{item.companyName}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </Stack>
          </Panel>
        </Stack>
      </Container>
    </Box>
  );
}
