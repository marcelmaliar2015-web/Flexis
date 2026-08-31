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
import { ApiError } from "@/shared/api/client";
import { getGoogleConnection, googleConnectionQueryKey } from "@/shared/api/google";
import {
  applyJobPipelineEntry,
  createJobPipelineEntry,
  deleteJobPipelineEntry,
  getJobPipelineBoard,
  jobPipelineQueryKey,
  updateJobPipelineEntry,
} from "@/shared/api/pipeline";
import type { JobPipelineEntry, JobPipelineWriteRequest } from "@/shared/types/pipeline";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  overflow: "hidden",
}));

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return "Request failed.";
}

function sourceChoiceValue(sourceId: string, sheetId: number): string {
  return `${sourceId}:${sheetId}`;
}

function parseSourceChoice(value: string): { sourceId: string; locationSheetId: number } | null {
  const separator = value.lastIndexOf(":");
  if (separator <= 0 || separator === value.length - 1) {
    return null;
  }

  const locationSheetId = Number(value.slice(separator + 1));
  if (!Number.isInteger(locationSheetId)) {
    return null;
  }

  return { sourceId: value.slice(0, separator), locationSheetId };
}

export function JobApplicationOperationsTab() {
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
  const [toDelete, setToDelete] = useState<JobPipelineEntry | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: (request: JobPipelineWriteRequest) => createJobPipelineEntry(request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: jobPipelineQueryKey });
      setCreating(false);
      setFormError(null);
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const updateRowMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: JobPipelineWriteRequest }) =>
      updateJobPipelineEntry(id, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: jobPipelineQueryKey });
    },
  });

  const applyMutation = useMutation({
    mutationFn: (id: string) => applyJobPipelineEntry(id),
    onSuccess: (result) => {
      if (result.added === 0 && result.skipped === 0) {
        setNotice("No listings to add from that source location.");
        return;
      }
      if (result.added === 0) {
        setNotice("Those listings are already on this profile.");
        return;
      }
      setNotice(
        result.skipped > 0
          ? `Added ${result.added}. Skipped ${result.skipped} already on this profile.`
          : `Added ${result.added}.`,
      );
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteJobPipelineEntry(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: jobPipelineQueryKey });
      setToDelete(null);
    },
  });

  const board = boardQuery.data;
  const sourceChoices =
    board?.sources.flatMap((source) =>
      source.locations.map((location) => ({
        value: sourceChoiceValue(source.id, location.sheetId),
        label: `${source.title} · ${location.name}`,
      })),
    ) ?? [];

  return (
    <Stack spacing={2}>
      {!connected ? (
        <Alert severity="info">Connect Gmail on the Settings tab before using the pipeline.</Alert>
      ) : null}
      {boardQuery.isError ? <Alert severity="error">{errorMessage(boardQuery.error)}</Alert> : null}
      {updateRowMutation.isError ? (
        <Alert severity="error">{errorMessage(updateRowMutation.error)}</Alert>
      ) : null}
      {applyMutation.isError ? <Alert severity="error">{errorMessage(applyMutation.error)}</Alert> : null}
      {deleteMutation.isError ? <Alert severity="error">{errorMessage(deleteMutation.error)}</Alert> : null}
      {notice ? <Alert severity="success">{notice}</Alert> : null}
      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
      >
        <Typography variant="h6" component="h2">
          Pipeline
        </Typography>
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
      <Panel>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Profile</TableCell>
                <TableCell>Source</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(board?.entries ?? []).map((entry) => (
                <TableRow key={entry.id} hover>
                  <TableCell>
                    <FormControl fullWidth size="small">
                      <InputLabel id={`pipeline-profile-${entry.id}`}>Profile</InputLabel>
                      <Select
                        labelId={`pipeline-profile-${entry.id}`}
                        label="Profile"
                        value={entry.profileId}
                        disabled={!connected || updateRowMutation.isPending}
                        onChange={(event) =>
                          updateRowMutation.mutate({
                            id: entry.id,
                            request: {
                              profileId: String(event.target.value),
                              sourceId: entry.sourceId,
                              locationSheetId: entry.locationSheetId,
                            },
                          })
                        }
                      >
                        {(board?.profiles ?? []).map((profile) => (
                          <MenuItem key={profile.id} value={profile.id}>
                            {profile.title}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </TableCell>
                  <TableCell>
                    <FormControl fullWidth size="small">
                      <InputLabel id={`pipeline-source-${entry.id}`}>Source</InputLabel>
                      <Select
                        labelId={`pipeline-source-${entry.id}`}
                        label="Source"
                        value={sourceChoiceValue(entry.sourceId, entry.locationSheetId)}
                        disabled={!connected || updateRowMutation.isPending}
                        onChange={(event) => {
                          const parsed = parseSourceChoice(String(event.target.value));
                          if (!parsed) {
                            return;
                          }
                          updateRowMutation.mutate({
                            id: entry.id,
                            request: {
                              profileId: entry.profileId,
                              sourceId: parsed.sourceId,
                              locationSheetId: parsed.locationSheetId,
                            },
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
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={0.5} sx={{ justifyContent: "center" }}>
                      <Button
                        disabled={!connected || applyMutation.isPending}
                        loading={applyMutation.isPending && applyMutation.variables === entry.id}
                        onClick={() => {
                          setNotice(null);
                          applyMutation.mutate(entry.id);
                        }}
                      >
                        Update
                      </Button>
                      <Button variant="text" disabled={deleteMutation.isPending} onClick={() => setToDelete(entry)}>
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
      {toDelete ? (
        <Dialog open onClose={() => setToDelete(null)} fullWidth maxWidth="xs">
          <DialogTitle>Delete pipeline entry</DialogTitle>
          <DialogContent>
            <Typography variant="body2">
              Remove this pipeline entry? Listings already on the profile sheet stay.
            </Typography>
          </DialogContent>
          <DialogActions>
            <Button variant="text" onClick={() => setToDelete(null)} disabled={deleteMutation.isPending}>
              Cancel
            </Button>
            <Button
              disabled={deleteMutation.isPending}
              loading={deleteMutation.isPending}
              onClick={() => deleteMutation.mutate(toDelete.id)}
            >
              Delete
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
