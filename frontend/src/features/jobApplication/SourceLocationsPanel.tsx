import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
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
import { SheetUrl } from "@/features/jobApplication/CatalogItemsPanel";
import { userFacingError } from "@/shared/api/errors";
import { isQueryLoading } from "@/shared/api/queryState";
import {
  createSourceLocation,
  deleteSourceLocation,
  jobCatalogQueryKey,
  listJobCatalogItems,
  listSourceLocations,
  sourceLocationsQueryKey,
  updateSourceLocation,
} from "@/shared/api/jobCatalog";
import { jobPipelineQueryKey } from "@/shared/api/pipeline";
import type { JobCatalogItem, SourceLocation } from "@/shared/types/jobCatalog";
import { refreshJobApplicationWorkspace } from "@/features/jobApplication/refreshWorkspace";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  overflow: "hidden",
}));

const Card = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2.5),
}));

function errorMessage(error: unknown): string | null {
  return userFacingError(error);
}

export function SourceLocationsPanel({ actionsEnabled }: { actionsEnabled: boolean }) {
  const sourcesQuery = useQuery({
    queryKey: jobCatalogQueryKey("sources"),
    queryFn: () => listJobCatalogItems("sources"),
  });

  const sources = sourcesQuery.data ?? [];
  const sourcesLoading = isQueryLoading(sourcesQuery.data, sourcesQuery.isPending);

  return (
    <Stack spacing={2}>
      <Typography variant="h6" component="h2">
        Locations
      </Typography>
      {sourcesLoading ? (
        <Typography variant="body2" color="text.secondary">
          Loading…
        </Typography>
      ) : sources.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          Create a source first. Each source workbook starts with a US tab. Add more locations here.
        </Typography>
      ) : (
        <Stack spacing={2}>
          {sources.map((item) => (
            <SourceLocationCard key={item.id} item={item} actionsEnabled={actionsEnabled} />
          ))}
        </Stack>
      )}
    </Stack>
  );
}

function SourceLocationCard({
  item,
  actionsEnabled,
}: {
  item: JobCatalogItem;
  actionsEnabled: boolean;
}) {
  const queryClient = useQueryClient();
  const locationsQuery = useQuery({
    queryKey: sourceLocationsQueryKey(item.id),
    queryFn: () => listSourceLocations(item.id),
    enabled: actionsEnabled && item.spreadsheetId.length > 0,
  });
  const [editor, setEditor] = useState<{ mode: "create" } | { mode: "edit"; location: SourceLocation } | null>(
    null,
  );
  const [toDelete, setToDelete] = useState<SourceLocation | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  async function refreshLocations() {
    await queryClient.invalidateQueries({ queryKey: sourceLocationsQueryKey(item.id) });
    await queryClient.invalidateQueries({ queryKey: jobPipelineQueryKey });
    await refreshJobApplicationWorkspace(queryClient);
  }

  const createMutation = useMutation({
    mutationFn: (name: string) => createSourceLocation(item.id, name),
    onSuccess: async () => {
      await refreshLocations();
      setEditor(null);
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const updateMutation = useMutation({
    mutationFn: ({ sheetId, name }: { sheetId: number; name: string }) =>
      updateSourceLocation(item.id, sheetId, name),
    onSuccess: async () => {
      await refreshLocations();
      setEditor(null);
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const deleteMutation = useMutation({
    mutationFn: (sheetId: number) => deleteSourceLocation(item.id, sheetId),
    onSuccess: async () => {
      await refreshLocations();
      setToDelete(null);
    },
  });

  return (
    <Card>
      <Stack spacing={2}>
        <Stack
          direction="row"
          spacing={2}
          sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
        >
          <Stack spacing={0.5} sx={{ minWidth: 0 }}>
            <Typography variant="subtitle1">{item.title}</Typography>
            {item.url ? <SheetUrl url={item.url} /> : null}
          </Stack>
          <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }}>
            {item.spreadsheetId.length > 0 ? (
              <Button
                disabled={!actionsEnabled}
                onClick={() => {
                  setFormError(null);
                  setEditor({ mode: "create" });
                }}
              >
                New location
              </Button>
            ) : null}
          </Stack>
        </Stack>
        {item.spreadsheetId.length > 0 && !actionsEnabled ? (
          <Typography variant="body2" color="text.secondary">
            Connect Gmail to load locations.
          </Typography>
        ) : item.spreadsheetId.length > 0 ? (
          <Panel>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Location</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {(locationsQuery.data ?? []).map((location) => (
                    <TableRow key={location.sheetId} hover>
                      <TableCell>{location.name}</TableCell>
                      <TableCell>
                        <Stack direction="row" spacing={0.5} sx={{ justifyContent: "center" }}>
                          <Button
                            variant="text"
                            disabled={!actionsEnabled}
                            onClick={() => {
                              setFormError(null);
                              setEditor({ mode: "edit", location });
                            }}
                          >
                            Rename
                          </Button>
                          <Button
                            variant="text"
                            disabled={!actionsEnabled}
                            onClick={() => setToDelete(location)}
                          >
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
        ) : (
          <Typography variant="body2" color="text.secondary">
            No Google Sheet
          </Typography>
        )}
      </Stack>
      {editor ? (
        <Dialog open onClose={() => setEditor(null)} fullWidth maxWidth="xs">
          <Box
            component="form"
            onSubmit={(event: FormEvent<HTMLFormElement>) => {
              event.preventDefault();
              const data = new FormData(event.currentTarget);
              const name = String(data.get("name") ?? "");
              if (editor.mode === "create") {
                createMutation.mutate(name);
                return;
              }
              updateMutation.mutate({ sheetId: editor.location.sheetId, name });
            }}
          >
            <DialogTitle>{editor.mode === "create" ? "New location" : "Rename location"}</DialogTitle>
            <DialogContent>
              <Stack spacing={2} sx={{ pt: 1 }}>
                {formError ? <Alert severity="error">{formError}</Alert> : null}
                <TextField
                  name="name"
                  label="Location"
                  defaultValue={editor.mode === "edit" ? editor.location.name : ""}
                  required
                  fullWidth
                  autoFocus
                />
              </Stack>
            </DialogContent>
            <DialogActions>
              <Button
                variant="text"
                onClick={() => setEditor(null)}
                disabled={createMutation.isPending || updateMutation.isPending}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={createMutation.isPending || updateMutation.isPending}
                loading={createMutation.isPending || updateMutation.isPending}
              >
                Save
              </Button>
            </DialogActions>
          </Box>
        </Dialog>
      ) : null}
      {toDelete ? (
        <Dialog open onClose={() => setToDelete(null)} fullWidth maxWidth="xs">
          <DialogTitle>Delete location</DialogTitle>
          <DialogContent>
            <Typography variant="body2">{`Delete ${toDelete.name}? This cannot be undone.`}</Typography>
          </DialogContent>
          <DialogActions>
            <Button variant="text" onClick={() => setToDelete(null)} disabled={deleteMutation.isPending}>
              Cancel
            </Button>
            <Button
              disabled={deleteMutation.isPending}
              loading={deleteMutation.isPending}
              onClick={() => deleteMutation.mutate(toDelete.sheetId)}
            >
              Delete
            </Button>
          </DialogActions>
        </Dialog>
      ) : null}
    </Card>
  );
}
