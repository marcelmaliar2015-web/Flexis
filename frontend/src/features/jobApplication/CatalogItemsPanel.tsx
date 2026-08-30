import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
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
import { useState, type FormEvent } from "react";
import { ApiError } from "@/shared/api/client";
import {
  createJobCatalogItem,
  deleteJobCatalogItem,
  jobCatalogQueryKey,
  listJobCatalogItems,
  updateJobCatalogItem,
} from "@/shared/api/jobCatalog";
import type { JobCatalogItem, JobCatalogKind } from "@/shared/types/jobCatalog";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  overflow: "hidden",
}));

type EditorState = { mode: "create" } | { mode: "edit"; item: JobCatalogItem };

type CatalogItemsPanelProps = {
  kind: JobCatalogKind;
  heading: string;
  itemLabel: string;
  actionsEnabled: boolean;
};

function formatCreatedAt(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(value));
}

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return "Request failed.";
}

export function CatalogItemsPanel({
  kind,
  heading,
  itemLabel,
  actionsEnabled,
}: CatalogItemsPanelProps) {
  const queryClient = useQueryClient();
  const queryKey = jobCatalogQueryKey(kind);
  const itemsQuery = useQuery({
    queryKey,
    queryFn: () => listJobCatalogItems(kind),
  });
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [itemToDelete, setItemToDelete] = useState<JobCatalogItem | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: (request: { title: string }) => createJobCatalogItem(kind, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey });
      setEditor(null);
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: { title: string } }) =>
      updateJobCatalogItem(kind, id, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey });
      setEditor(null);
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteJobCatalogItem(kind, id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey });
      setItemToDelete(null);
    },
  });

  return (
    <Stack spacing={2}>
      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
      >
        <Typography variant="h6" component="h2">
          {heading}
        </Typography>
        <Button
          disabled={!actionsEnabled}
          onClick={() => {
            setFormError(null);
            setEditor({ mode: "create" });
          }}
        >
          {`New ${itemLabel}`}
        </Button>
      </Stack>
      {itemsQuery.isError ? <Alert severity="error">{errorMessage(itemsQuery.error)}</Alert> : null}
      {deleteMutation.isError ? (
        <Alert severity="error">{errorMessage(deleteMutation.error)}</Alert>
      ) : null}
      <Panel>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Title</TableCell>
                <TableCell>Created</TableCell>
                <TableCell>Sheet</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(itemsQuery.data ?? []).map((item) => (
                <TableRow key={item.id} hover>
                  <TableCell>{item.title}</TableCell>
                  <TableCell>{formatCreatedAt(item.createdAt)}</TableCell>
                  <TableCell>
                    {item.url && actionsEnabled ? (
                      <Link href={item.url} target="_blank" rel="noopener noreferrer">
                        Open sheet
                      </Link>
                    ) : item.url ? (
                      "Open sheet"
                    ) : (
                      "—"
                    )}
                  </TableCell>
                  <TableCell align="right">
                    <Stack direction="row" spacing={0.5} sx={{ justifyContent: "flex-end" }}>
                      <Button
                        variant="text"
                        disabled={!actionsEnabled}
                        onClick={() => {
                          setFormError(null);
                          setEditor({ mode: "edit", item });
                        }}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="text"
                        disabled={!actionsEnabled}
                        onClick={() => setItemToDelete(item)}
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
      {editor ? (
        <CatalogEditorDialog
          editor={editor}
          itemLabel={itemLabel}
          error={formError}
          isSaving={createMutation.isPending || updateMutation.isPending}
          onClose={() => setEditor(null)}
          onCreate={(request) => createMutation.mutate(request)}
          onUpdate={(id, request) => updateMutation.mutate({ id, request })}
        />
      ) : null}
      {itemToDelete ? (
        <Dialog open onClose={() => setItemToDelete(null)} fullWidth maxWidth="xs">
          <DialogTitle>{`Delete ${itemLabel}`}</DialogTitle>
          <DialogContent>
            <Typography variant="body2">
              {`Delete ${itemToDelete.title}? This cannot be undone.`}
            </Typography>
          </DialogContent>
          <DialogActions>
            <Button variant="text" onClick={() => setItemToDelete(null)} disabled={deleteMutation.isPending}>
              Cancel
            </Button>
            <Button
              disabled={deleteMutation.isPending}
              loading={deleteMutation.isPending}
              onClick={() => deleteMutation.mutate(itemToDelete.id)}
            >
              Delete
            </Button>
          </DialogActions>
        </Dialog>
      ) : null}
    </Stack>
  );
}

type CatalogEditorDialogProps = {
  editor: EditorState;
  itemLabel: string;
  error: string | null;
  isSaving: boolean;
  onClose: () => void;
  onCreate: (request: { title: string }) => void;
  onUpdate: (id: string, request: { title: string }) => void;
};

function CatalogEditorDialog({
  editor,
  itemLabel,
  error,
  isSaving,
  onClose,
  onCreate,
  onUpdate,
}: CatalogEditorDialogProps) {
  const isEdit = editor.mode === "edit";
  const [title, setTitle] = useState(isEdit ? editor.item.title : "");

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (editor.mode === "create") {
      onCreate({ title });
      return;
    }

    onUpdate(editor.item.id, { title });
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{isEdit ? `Edit ${itemLabel}` : `New ${itemLabel}`}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {error ? <Alert severity="error">{error}</Alert> : null}
            <TextField
              label="Title"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              required
              fullWidth
            />
            <Typography variant="body2" color="text.secondary">
              {isEdit
                ? "Saving a new title also renames the Google Sheet."
                : `Creates a Google Sheet. Connect Gmail first. ${
                    itemLabel === "profile"
                      ? "The tab is named after this title."
                      : "The first location tab is US."
                  }`}
            </Typography>
            {isEdit && editor.item.url ? (
              <Link href={editor.item.url} target="_blank" rel="noopener noreferrer">
                Open sheet
              </Link>
            ) : null}
            {isEdit ? (
              <Typography variant="body2" color="text.secondary">
                {`Created ${formatCreatedAt(editor.item.createdAt)}`}
              </Typography>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button variant="text" onClick={onClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSaving} loading={isSaving}>
            Save
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}
