import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import FormControl from "@mui/material/FormControl";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import Select from "@mui/material/Select";
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
import { useState, type FormEvent } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { getGoogleConnection, googleConnectionQueryKey } from "@/shared/api/google";
import { googleSyncIntervalMs } from "@/shared/api/googleSync";
import {
  createProfileBannedCompany,
  deleteProfileBannedCompany,
  getProfileBannedMatches,
  listProfileBannedCompanies,
  profileBannedMatchesQueryKey,
  profileBannedQueryKey,
  updateProfileBannedCompany,
} from "@/shared/api/jobCatalog";
import {
  applyJobPipelineEntry,
  deleteJobPipelineEntry,
  forwardJobPipelineEntry,
  getJobPipelineBoard,
  jobPipelineQueryKey,
  updateJobPipelineEntry,
} from "@/shared/api/pipeline";
import { appPaths } from "@/shared/config/paths";
import type { ProfileBannedCompany, ProfileBannedMatch } from "@/shared/types/jobCatalog";
import type { JobPipelineWriteRequest } from "@/shared/types/pipeline";
import { ProfileInfoPanel } from "@/features/jobApplication/ProfileInfoPanel";
import {
  errorMessage,
  forwardNotice,
  listingsNotice,
  parseSourceChoice,
  sourceChoiceValue,
  sourceChoicesFromBoard,
} from "@/features/jobApplication/pipelineUi";
import { refreshJobApplicationWorkspace } from "@/features/jobApplication/refreshWorkspace";

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
  overflow: "hidden",
}));

export function JobApplicationPipelineEntryPage() {
  const { entryId } = useParams<{ entryId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const connectionQuery = useQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });
  const connected = connectionQuery.data?.connected === true;
  const boardQuery = useQuery({
    queryKey: jobPipelineQueryKey,
    queryFn: getJobPipelineBoard,
    enabled: Boolean(entryId),
  });
  const entry = boardQuery.data?.entries.find((item) => item.id === entryId);
  const profileId = entry?.profileId ?? "";
  const board = boardQuery.data;
  const sourceChoices = board ? sourceChoicesFromBoard(board) : [];

  const bannedQuery = useQuery({
    queryKey: profileBannedQueryKey(profileId),
    queryFn: () => listProfileBannedCompanies(profileId),
    enabled: Boolean(profileId),
  });
  const matchesQuery = useQuery({
    queryKey: profileBannedMatchesQueryKey(profileId),
    queryFn: () => getProfileBannedMatches(profileId),
    enabled: connected && Boolean(profileId),
    refetchInterval: googleSyncIntervalMs,
    refetchOnWindowFocus: true,
  });
  const [notice, setNotice] = useState<string | null>(null);
  const [toDelete, setToDelete] = useState(false);
  const [banEditor, setBanEditor] = useState<
    { mode: "create" } | { mode: "edit"; item: ProfileBannedCompany } | null
  >(null);
  const [banToDelete, setBanToDelete] = useState<ProfileBannedCompany | null>(null);
  const [banError, setBanError] = useState<string | null>(null);

  const updateRowMutation = useMutation({
    mutationFn: (request: JobPipelineWriteRequest) => updateJobPipelineEntry(entryId ?? "", request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: jobPipelineQueryKey });
      await queryClient.invalidateQueries({ queryKey: profileBannedMatchesQueryKey(profileId) });
      await refreshJobApplicationWorkspace(queryClient);
    },
  });

  const applyMutation = useMutation({
    mutationFn: () => applyJobPipelineEntry(entryId ?? ""),
    onSuccess: async (result) => {
      setNotice(listingsNotice(result, false));
      await queryClient.invalidateQueries({ queryKey: profileBannedMatchesQueryKey(profileId) });
      await refreshJobApplicationWorkspace(queryClient);
    },
  });

  const forwardMutation = useMutation({
    mutationFn: () => forwardJobPipelineEntry(entryId ?? ""),
    onSuccess: async (result) => {
      setNotice(forwardNotice(result));
      await queryClient.invalidateQueries({ queryKey: profileBannedMatchesQueryKey(profileId) });
      await refreshJobApplicationWorkspace(queryClient);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteJobPipelineEntry(entryId ?? ""),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: jobPipelineQueryKey });
      await refreshJobApplicationWorkspace(queryClient);
      navigate(appPaths.jobApplication);
    },
  });

  const createBanMutation = useMutation({
    mutationFn: (companyName: string) => createProfileBannedCompany(profileId, { companyName }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: profileBannedQueryKey(profileId) });
      await queryClient.invalidateQueries({ queryKey: profileBannedMatchesQueryKey(profileId) });
      setBanEditor(null);
      setBanError(null);
      await refreshJobApplicationWorkspace(queryClient);
    },
    onError: (error) => setBanError(errorMessage(error)),
  });

  const updateBanMutation = useMutation({
    mutationFn: ({ companyId, companyName }: { companyId: string; companyName: string }) =>
      updateProfileBannedCompany(profileId, companyId, { companyName }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: profileBannedQueryKey(profileId) });
      await queryClient.invalidateQueries({ queryKey: profileBannedMatchesQueryKey(profileId) });
      setBanEditor(null);
      setBanError(null);
      await refreshJobApplicationWorkspace(queryClient);
    },
    onError: (error) => setBanError(errorMessage(error)),
  });

  const deleteBanMutation = useMutation({
    mutationFn: (companyId: string) => deleteProfileBannedCompany(profileId, companyId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: profileBannedQueryKey(profileId) });
      await queryClient.invalidateQueries({ queryKey: profileBannedMatchesQueryKey(profileId) });
      setBanToDelete(null);
      await refreshJobApplicationWorkspace(queryClient);
    },
  });

  const actionsBusy = applyMutation.isPending || forwardMutation.isPending;
  const bans = bannedQuery.data ?? [];
  const matches = matchesQuery.data?.matches ?? [];

  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="lg">
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Button component={RouterLink} to={appPaths.jobApplication} variant="text">
              Back
            </Button>
            <Typography variant="overline" color="secondary">
              Job Application
            </Typography>
            <Typography variant="h4" component="h1">
              Pipeline entry
            </Typography>
            <AccentRule />
          </Stack>
          {boardQuery.isSuccess && !entry ? (
            <Alert severity="error">Pipeline entry was not found.</Alert>
          ) : null}
          {notice ? <Alert severity="success">{notice}</Alert> : null}
          {entry && board ? (
            <>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                <FormControl fullWidth>
                  <InputLabel id="pipeline-entry-profile">Profile</InputLabel>
                  <Select
                    labelId="pipeline-entry-profile"
                    label="Profile"
                    value={entry.profileId}
                    disabled={!connected || updateRowMutation.isPending || actionsBusy}
                    onChange={(event) =>
                      updateRowMutation.mutate({
                        profileId: String(event.target.value),
                        sourceId: entry.sourceId,
                        locationSheetId: entry.locationSheetId,
                      })
                    }
                  >
                    {board.profiles.map((profile) => (
                      <MenuItem key={profile.id} value={profile.id}>
                        {profile.title}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <FormControl fullWidth>
                  <InputLabel id="pipeline-entry-source">Source</InputLabel>
                  <Select
                    labelId="pipeline-entry-source"
                    label="Source"
                    value={sourceChoiceValue(entry.sourceId, entry.locationSheetId)}
                    disabled={!connected || updateRowMutation.isPending || actionsBusy}
                    onChange={(event) => {
                      const parsed = parseSourceChoice(String(event.target.value));
                      if (!parsed) {
                        return;
                      }
                      updateRowMutation.mutate({
                        profileId: entry.profileId,
                        sourceId: parsed.sourceId,
                        locationSheetId: parsed.locationSheetId,
                      });
                    }}
                  >
                    {sourceChoices.map((choice) => (
                      <MenuItem key={choice.value} value={choice.value}>
                        {choice.label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Stack>
              <Stack direction="row" spacing={1}>
                <Button
                  disabled={!connected || actionsBusy}
                  loading={applyMutation.isPending}
                  onClick={() => {
                    setNotice(null);
                    applyMutation.mutate();
                  }}
                >
                  Update
                </Button>
                <Button
                  disabled={!connected || actionsBusy}
                  loading={forwardMutation.isPending}
                  onClick={() => {
                    setNotice(null);
                    forwardMutation.mutate();
                  }}
                >
                  Forward
                </Button>
                <Button variant="text" disabled={deleteMutation.isPending || actionsBusy} onClick={() => setToDelete(true)}>
                  Delete
                </Button>
              </Stack>
              <ProfileInfoPanel actionsEnabled={connected} profileId={profileId} />
              <Stack
                direction="row"
                spacing={2}
                sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
              >
                <Typography variant="h6" component="h2">
                  Banned companies
                </Typography>
                <Button
                  disabled={!connected}
                  onClick={() => {
                    setBanError(null);
                    setBanEditor({ mode: "create" });
                  }}
                >
                  New
                </Button>
              </Stack>
              <Panel>
                <TableContainer>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Company</TableCell>
                        <TableCell>Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {bans.map((item) => (
                        <TableRow key={item.id} hover>
                          <TableCell>{item.companyName}</TableCell>
                          <TableCell>
                            <Stack direction="row" spacing={0.5} sx={{ justifyContent: "center" }}>
                              <Button
                                variant="text"
                                onClick={() => {
                                  setBanError(null);
                                  setBanEditor({ mode: "edit", item });
                                }}
                              >
                                Edit
                              </Button>
                              <Button variant="text" onClick={() => setBanToDelete(item)}>
                                Delete
                              </Button>
                            </Stack>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </Panel>
              <Typography variant="h6" component="h2">
                Banned matches
              </Typography>
              {!connected ? (
                <Alert severity="info">Connect Gmail on Settings to scan sheets.</Alert>
              ) : null}
              {connected && bans.length === 0 ? (
                <Alert severity="info">Add banned companies to scan the profile main sheet.</Alert>
              ) : null}
              {connected && bans.length > 0 && matches.length === 0 && !matchesQuery.isError ? (
                <Alert severity="success">No banned companies on the profile main sheet.</Alert>
              ) : null}
              {matches.length > 0 ? (
                <Panel>
                  <TableContainer>
                    <Table>
                      <TableHead>
                        <TableRow>
                          <TableCell>Company</TableCell>
                          <TableCell>Position</TableCell>
                          <TableCell>Ban</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {matches.map((match) => (
                          <MatchRow key={`${match.link}-${match.companyName}-${match.position}`} match={match} />
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Panel>
              ) : null}
            </>
          ) : null}
        </Stack>
      </Container>
      {toDelete ? (
        <Dialog open onClose={() => setToDelete(false)} fullWidth maxWidth="xs">
          <DialogTitle>Delete pipeline entry</DialogTitle>
          <DialogContent>
            <Typography variant="body2">
              Remove this pipeline entry? Listings already on the profile sheet stay.
            </Typography>
          </DialogContent>
          <DialogActions>
            <Button variant="text" onClick={() => setToDelete(false)} disabled={deleteMutation.isPending}>
              Cancel
            </Button>
            <Button
              disabled={deleteMutation.isPending}
              loading={deleteMutation.isPending}
              onClick={() => deleteMutation.mutate()}
            >
              Delete
            </Button>
          </DialogActions>
        </Dialog>
      ) : null}
      {banEditor ? (
        <BannedCompanyDialog
          title={banEditor.mode === "create" ? "New banned company" : "Edit banned company"}
          initialName={banEditor.mode === "edit" ? banEditor.item.companyName : ""}
          error={banError}
          isSaving={createBanMutation.isPending || updateBanMutation.isPending}
          onClose={() => setBanEditor(null)}
          onSave={(companyName) => {
            if (banEditor.mode === "create") {
              createBanMutation.mutate(companyName);
              return;
            }
            updateBanMutation.mutate({ companyId: banEditor.item.id, companyName });
          }}
        />
      ) : null}
      {banToDelete ? (
        <Dialog open onClose={() => setBanToDelete(null)} fullWidth maxWidth="xs">
          <DialogTitle>Delete banned company</DialogTitle>
          <DialogContent>
            <Typography variant="body2">Remove {banToDelete.companyName} from this profile?</Typography>
          </DialogContent>
          <DialogActions>
            <Button variant="text" onClick={() => setBanToDelete(null)} disabled={deleteBanMutation.isPending}>
              Cancel
            </Button>
            <Button
              disabled={deleteBanMutation.isPending}
              loading={deleteBanMutation.isPending}
              onClick={() => deleteBanMutation.mutate(banToDelete.id)}
            >
              Delete
            </Button>
          </DialogActions>
        </Dialog>
      ) : null}
    </Box>
  );
}

function MatchRow({ match }: { match: ProfileBannedMatch }) {
  return (
    <TableRow hover>
      <TableCell>{match.companyName}</TableCell>
      <TableCell>{match.position}</TableCell>
      <TableCell>{match.matchedBan}</TableCell>
    </TableRow>
  );
}

function BannedCompanyDialog({
  title,
  initialName,
  error,
  isSaving,
  onClose,
  onSave,
}: {
  title: string;
  initialName: string;
  error: string | null;
  isSaving: boolean;
  onClose: () => void;
  onSave: (companyName: string) => void;
}) {
  const [companyName, setCompanyName] = useState(initialName);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    onSave(companyName);
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{title}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {error ? <Alert severity="error">{error}</Alert> : null}
            <TextField
              autoFocus
              fullWidth
              required
              label="Company name"
              value={companyName}
              onChange={(event) => setCompanyName(event.target.value)}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button variant="text" onClick={onClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSaving || companyName.trim().length === 0} loading={isSaving}>
            Save
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}
