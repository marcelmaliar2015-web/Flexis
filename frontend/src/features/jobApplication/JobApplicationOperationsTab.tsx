import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
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
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { getGoogleConnection, googleConnectionQueryKey } from "@/shared/api/google";
import {
  applyAllJobPipelineEntries,
  createJobPipelineEntry,
  deleteAllJobPipelineEntries,
  forwardAllJobPipelineEntries,
  getJobPipelineBoard,
  jobPipelineQueryKey,
} from "@/shared/api/pipeline";
import { appPaths } from "@/shared/config/paths";
import type { JobPipelineWriteRequest } from "@/shared/types/pipeline";
import {
  errorMessage,
  listingsNotice,
  parseSourceChoice,
  profileTitle,
  sourceChoicesFromBoard,
  sourceLabel,
} from "@/features/jobApplication/pipelineUi";
import { refreshJobApplicationWorkspace } from "@/features/jobApplication/refreshWorkspace";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  overflow: "hidden",
}));

const ClickableRow = styled(TableRow)({
  cursor: "pointer",
});

export function JobApplicationOperationsTab() {
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
    enabled: connected,
  });
  const [creating, setCreating] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [deleteAllOpen, setDeleteAllOpen] = useState(false);

  const createMutation = useMutation({
    mutationFn: (request: JobPipelineWriteRequest) => createJobPipelineEntry(request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: jobPipelineQueryKey });
      await refreshJobApplicationWorkspace(queryClient);
      setCreating(false);
      setFormError(null);
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const applyAllMutation = useMutation({
    mutationFn: applyAllJobPipelineEntries,
    onSuccess: async (result) => {
      setNotice(listingsNotice(result, true));
      await refreshJobApplicationWorkspace(queryClient);
    },
  });

  const forwardAllMutation = useMutation({
    mutationFn: forwardAllJobPipelineEntries,
    onSuccess: async (result) => {
      setNotice(
        result.forwarded === 1
          ? "Forwarded 1 profile sheet."
          : `Forwarded ${result.forwarded} profile sheets.`,
      );
      await refreshJobApplicationWorkspace(queryClient);
    },
  });

  const deleteAllMutation = useMutation({
    mutationFn: deleteAllJobPipelineEntries,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: jobPipelineQueryKey });
      await refreshJobApplicationWorkspace(queryClient);
      setDeleteAllOpen(false);
    },
  });

  const board = boardQuery.data;
  const entries = board?.entries ?? [];
  const actionsBusy = applyAllMutation.isPending || forwardAllMutation.isPending || deleteAllMutation.isPending;
  const sourceChoices = board ? sourceChoicesFromBoard(board) : [];

  return (
    <Stack spacing={2}>
      {!connected ? (
        <Alert severity="info">Connect Gmail on the Settings tab before using the pipeline.</Alert>
      ) : null}
      {boardQuery.isError ? <Alert severity="error">{errorMessage(boardQuery.error)}</Alert> : null}
      {applyAllMutation.isError ? <Alert severity="error">{errorMessage(applyAllMutation.error)}</Alert> : null}
      {forwardAllMutation.isError ? (
        <Alert severity="error">{errorMessage(forwardAllMutation.error)}</Alert>
      ) : null}
      {deleteAllMutation.isError ? (
        <Alert severity="error">{errorMessage(deleteAllMutation.error)}</Alert>
      ) : null}
      {notice ? <Alert severity="success">{notice}</Alert> : null}
      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
      >
        <Typography variant="h6" component="h2">
          Pipeline
        </Typography>
        <Stack direction="row" spacing={1}>
          <Button
            disabled={!connected || entries.length === 0 || actionsBusy}
            loading={applyAllMutation.isPending}
            onClick={() => {
              setNotice(null);
              applyAllMutation.mutate();
            }}
          >
            Update All
          </Button>
          <Button
            disabled={!connected || entries.length === 0 || actionsBusy}
            loading={forwardAllMutation.isPending}
            onClick={() => {
              setNotice(null);
              forwardAllMutation.mutate();
            }}
          >
            Forward All
          </Button>
          <Button
            disabled={!connected || entries.length === 0 || actionsBusy}
            onClick={() => setDeleteAllOpen(true)}
          >
            Delete All
          </Button>
          <Button
            disabled={!connected || sourceChoices.length === 0 || (board?.profiles.length ?? 0) === 0}
            onClick={() => {
              setFormError(null);
              setCreating(true);
            }}
          >
            New
          </Button>
        </Stack>
      </Stack>
      <Panel>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Profile</TableCell>
                <TableCell>Source</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {entries.map((entry) => (
                <ClickableRow
                  key={entry.id}
                  hover
                  onClick={() => navigate(appPaths.jobApplicationPipeline(entry.id))}
                >
                  <TableCell>{board ? profileTitle(board, entry.profileId) : entry.profileId}</TableCell>
                  <TableCell>
                    {board
                      ? sourceLabel(board, entry.sourceId, entry.locationSheetId, entry.locationName)
                      : entry.locationName}
                  </TableCell>
                </ClickableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Panel>
      {creating && board ? (
        <CreatePipelineDialog
          profiles={board.profiles}
          sourceChoices={sourceChoices}
          error={formError}
          isSaving={createMutation.isPending}
          onClose={() => setCreating(false)}
          onCreate={(request) => createMutation.mutate(request)}
        />
      ) : null}
      {deleteAllOpen ? (
        <Dialog open onClose={() => setDeleteAllOpen(false)} fullWidth maxWidth="xs">
          <DialogTitle>Delete all pipeline entries</DialogTitle>
          <DialogContent>
            <Typography variant="body2">
              Remove every pipeline entry? Listings already on profile sheets stay.
            </Typography>
          </DialogContent>
          <DialogActions>
            <Button variant="text" onClick={() => setDeleteAllOpen(false)} disabled={deleteAllMutation.isPending}>
              Cancel
            </Button>
            <Button
              disabled={deleteAllMutation.isPending}
              loading={deleteAllMutation.isPending}
              onClick={() => deleteAllMutation.mutate()}
            >
              Delete All
            </Button>
          </DialogActions>
        </Dialog>
      ) : null}
    </Stack>
  );
}

function CreatePipelineDialog({
  profiles,
  sourceChoices,
  error,
  isSaving,
  onClose,
  onCreate,
}: {
  profiles: { id: string; title: string }[];
  sourceChoices: { value: string; label: string }[];
  error: string | null;
  isSaving: boolean;
  onClose: () => void;
  onCreate: (request: JobPipelineWriteRequest) => void;
}) {
  const [profileId, setProfileId] = useState(profiles[0]?.id ?? "");
  const [sourceValue, setSourceValue] = useState(sourceChoices[0]?.value ?? "");

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = parseSourceChoice(sourceValue);
    if (!parsed) {
      return;
    }

    onCreate({
      profileId,
      sourceId: parsed.sourceId,
      locationSheetId: parsed.locationSheetId,
    });
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>New pipeline entry</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {error ? <Alert severity="error">{error}</Alert> : null}
            <FormControl fullWidth>
              <InputLabel id="pipeline-new-profile">Profile</InputLabel>
              <Select
                labelId="pipeline-new-profile"
                label="Profile"
                value={profileId}
                onChange={(event) => setProfileId(String(event.target.value))}
              >
                {profiles.map((profile) => (
                  <MenuItem key={profile.id} value={profile.id}>
                    {profile.title}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <FormControl fullWidth>
              <InputLabel id="pipeline-new-source">Source</InputLabel>
              <Select
                labelId="pipeline-new-source"
                label="Source"
                value={sourceValue}
                onChange={(event) => setSourceValue(String(event.target.value))}
              >
                {sourceChoices.map((choice) => (
                  <MenuItem key={choice.value} value={choice.value}>
                    {choice.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button variant="text" onClick={onClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSaving || !profileId || !sourceValue} loading={isSaving}>
            Save
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}
