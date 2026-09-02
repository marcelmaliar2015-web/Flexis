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
import { isQueryLoading } from "@/shared/api/queryState";
import {
  createJobPipelineEntry,
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
import type { usePipelineBulkRun } from "@/features/jobApplication/usePipelineBulkRun";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  overflow: "hidden",
}));

const ClickableRow = styled(TableRow)({
  cursor: "pointer",
});

type PipelineBulkRun = ReturnType<typeof usePipelineBulkRun>;

type JobApplicationOperationsTabProps = {
  bulkRun: PipelineBulkRun;
};

export function JobApplicationOperationsTab({ bulkRun }: JobApplicationOperationsTabProps) {
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

  const board = boardQuery.data;
  const boardLoading = isQueryLoading(boardQuery.data, boardQuery.isPending);
  const entries = board?.entries ?? [];
  const actionsBusy = bulkRun.running || createMutation.isPending;
  const sourceChoices = board ? sourceChoicesFromBoard(board) : [];

  async function handleUpdateAll() {
    if (!board) {
      return;
    }

    setNotice(null);
    bulkRun.clearError();
    const result = await bulkRun.startUpdateAll(board);
    if (result) {
      setNotice(listingsNotice(result, true));
    }
  }

  async function handleForwardAll() {
    if (!board) {
      return;
    }

    setNotice(null);
    bulkRun.clearError();
    const forwarded = await bulkRun.startForwardAll(board);
    if (forwarded !== null) {
      setNotice(
        forwarded === 1 ? "Forwarded 1 profile sheet." : `Forwarded ${forwarded} profile sheets.`,
      );
    }
  }

  async function handleDeleteAll() {
    setNotice(null);
    bulkRun.clearError();
    const removed = await bulkRun.startDeleteAll(entries.length);
    if (removed) {
      setDeleteAllOpen(false);
      setNotice(
        entries.length === 1
          ? "Removed 1 pipeline entry."
          : `Removed ${entries.length} pipeline entries.`,
      );
    }
  }

  return (
    <Stack spacing={2}>
      {!connected ? (
        <Alert severity="info">Connect Gmail on the Settings tab before using the pipeline.</Alert>
      ) : null}
      {notice ? <Alert severity="success">{notice}</Alert> : null}
      {bulkRun.error ? <Alert severity="error">{bulkRun.error}</Alert> : null}
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
            loading={bulkRun.running && bulkRun.session?.action === "update"}
            onClick={() => {
              void handleUpdateAll();
            }}
          >
            Update All
          </Button>
          <Button
            disabled={!connected || entries.length === 0 || actionsBusy}
            loading={bulkRun.running && bulkRun.session?.action === "forward"}
            onClick={() => {
              void handleForwardAll();
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
            disabled={!connected || sourceChoices.length === 0 || (board?.profiles.length ?? 0) === 0 || actionsBusy}
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
              {boardLoading ? (
                <TableRow>
                  <TableCell colSpan={2}>
                    <Typography variant="body2" color="text.secondary">
                      Loading…
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : entries.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={2}>
                    <Typography variant="body2" color="text.secondary">
                      No pipeline rows yet.
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                entries.map((entry) => (
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
              ))
              )}
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
        <Dialog open onClose={() => !bulkRun.running && setDeleteAllOpen(false)} fullWidth maxWidth="xs">
          <DialogTitle>Delete all pipeline entries</DialogTitle>
          <DialogContent>
            <Typography variant="body2">
              Remove every pipeline entry? Listings already on profile sheets stay.
            </Typography>
          </DialogContent>
          <DialogActions>
            <Button variant="text" onClick={() => setDeleteAllOpen(false)} disabled={bulkRun.running}>
              Cancel
            </Button>
            <Button
              disabled={bulkRun.running}
              loading={bulkRun.running && bulkRun.session?.action === "delete"}
              onClick={() => {
                void handleDeleteAll();
              }}
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
