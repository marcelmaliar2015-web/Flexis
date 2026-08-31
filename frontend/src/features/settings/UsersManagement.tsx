import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import FormControl from "@mui/material/FormControl";
import FormControlLabel from "@mui/material/FormControlLabel";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import Select from "@mui/material/Select";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
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
import { userFacingError } from "@/shared/api/errors";
import {
  createUser,
  deleteUser,
  listUsers,
  updateUser,
  usersQueryKey,
  type CreateUserRequest,
  type UpdateUserRequest,
} from "@/shared/api/users";
import { useAuth } from "@/shared/auth/AuthProvider";
import type { UserDto, UserRole } from "@/shared/types/user";

const roles: UserRole[] = ["Admin", "User", "Viewer"];

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  overflow: "hidden",
}));

type EditorState = { mode: "create" } | { mode: "edit"; user: UserDto };

export function UsersManagement() {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const usersQuery = useQuery({
    queryKey: usersQueryKey,
    queryFn: listUsers,
  });
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [userToDelete, setUserToDelete] = useState<UserDto | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: createUser,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: usersQueryKey });
      setEditor(null);
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateUserRequest }) =>
      updateUser(id, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: usersQueryKey });
      setEditor(null);
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteUser(id),
    onSuccess: async (_result, id) => {
      if (id === auth.user?.id) {
        auth.signOut();
        return;
      }

      await queryClient.invalidateQueries({ queryKey: usersQueryKey });
      setUserToDelete(null);
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const others = (usersQuery.data ?? []).filter((user) => user.id !== auth.user?.id);

  return (
    <Stack spacing={2}>
      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
      >
        <Stack spacing={0.5}>
          <Typography variant="h6" component="h2">
            Users
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Other Flexis accounts. Your profile is in Your account above. Assign Admin, User, or Viewer.
          </Typography>
        </Stack>
        <Button
          onClick={() => {
            setFormError(null);
            setEditor({ mode: "create" });
          }}
        >
          New user
        </Button>
      </Stack>
      <Panel>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Email</TableCell>
                <TableCell>Role</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {others.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5}>
                    <Typography variant="body2" color="text.secondary">
                      No other users yet.
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                others.map((user) => (
                <TableRow key={user.id} hover>
                  <TableCell>{user.displayName}</TableCell>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>{user.role}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      color={user.isActive ? "success" : "default"}
                      label={user.isActive ? "Active" : "Inactive"}
                    />
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={0.5} sx={{ justifyContent: "center" }}>
                      <Button
                        variant="text"
                        onClick={() => {
                          setFormError(null);
                          setEditor({ mode: "edit", user });
                        }}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="text"
                        onClick={() => {
                          setFormError(null);
                          setUserToDelete(user);
                        }}
                      >
                        Delete
                      </Button>
                    </Stack>
                  </TableCell>
                </TableRow>
              ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Panel>
      {editor ? (
        <UserEditorDialog
          editor={editor}
          error={formError}
          isSaving={createMutation.isPending || updateMutation.isPending}
          onClose={() => setEditor(null)}
          onCreate={(request) => createMutation.mutate(request)}
          onUpdate={(id, request) => updateMutation.mutate({ id, request })}
        />
      ) : null}
      {userToDelete ? (
        <Dialog open onClose={() => setUserToDelete(null)} fullWidth maxWidth="xs">
          <DialogTitle>Delete user</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 0.5 }}>
              {formError ? <Alert severity="error">{formError}</Alert> : null}
              <Typography variant="body2">
                {`Delete ${userToDelete.displayName}? This cannot be undone.`}
              </Typography>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button
              variant="text"
              onClick={() => setUserToDelete(null)}
              disabled={deleteMutation.isPending}
            >
              Cancel
            </Button>
            <Button
              disabled={deleteMutation.isPending}
              loading={deleteMutation.isPending}
              onClick={() => deleteMutation.mutate(userToDelete.id)}
            >
              Delete
            </Button>
          </DialogActions>
        </Dialog>
      ) : null}
    </Stack>
  );
}

type UserEditorDialogProps = {
  editor: EditorState;
  error: string | null;
  isSaving: boolean;
  onClose: () => void;
  onCreate: (request: CreateUserRequest) => void;
  onUpdate: (id: string, request: UpdateUserRequest) => void;
};

function UserEditorDialog({
  editor,
  error,
  isSaving,
  onClose,
  onCreate,
  onUpdate,
}: UserEditorDialogProps) {
  const isEdit = editor.mode === "edit";
  const [email, setEmail] = useState(isEdit ? editor.user.email : "");
  const [displayName, setDisplayName] = useState(isEdit ? editor.user.displayName : "");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<UserRole>(isEdit ? editor.user.role : "User");
  const [isActive, setIsActive] = useState(isEdit ? editor.user.isActive : true);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (editor.mode === "create") {
      onCreate({ email, displayName, password, role });
      return;
    }

    onUpdate(editor.user.id, {
      displayName,
      role,
      isActive,
      password: password.length > 0 ? password : null,
    });
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <DialogTitle>{isEdit ? "Edit user" : "New user"}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {error ? <Alert severity="error">{error}</Alert> : null}
            <TextField
              label="Email"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required={!isEdit}
              disabled={isEdit}
              fullWidth
            />
            <TextField
              label="Display name"
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              required
              fullWidth
            />
            <TextField
              label={isEdit ? "New password (optional)" : "Password"}
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required={!isEdit}
              fullWidth
            />
            <FormControl fullWidth>
              <InputLabel id="user-role-label">Role</InputLabel>
              <Select
                labelId="user-role-label"
                label="Role"
                value={role}
                onChange={(event) => setRole(event.target.value as UserRole)}
              >
                {roles.map((item) => (
                  <MenuItem key={item} value={item}>
                    {item}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            {isEdit ? (
              <FormControlLabel
                control={
                  <Switch
                    checked={isActive}
                    onChange={(event) => setIsActive(event.target.checked)}
                  />
                }
                label="Active"
              />
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

function errorMessage(error: unknown): string | null {
  return userFacingError(error);
}
