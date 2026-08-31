import Alert from "@mui/material/Alert";
import Avatar from "@mui/material/Avatar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { updateCurrentUser } from "@/shared/api/auth";
import { userFacingError } from "@/shared/api/errors";
import { usersQueryKey } from "@/shared/api/users";
import { useAuth } from "@/shared/auth/AuthProvider";
import { userInitials } from "@/shared/auth/userInitials";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3),
}));

const ProfileAvatar = styled(Avatar)(({ theme }) => ({
  width: 56,
  height: 56,
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  fontSize: "1.25rem",
  fontWeight: 600,
}));

function errorMessage(error: unknown): string | null {
  return userFacingError(error);
}

export function AccountProfile() {
  const auth = useAuth();
  const user = auth.user;
  const queryClient = useQueryClient();
  const [displayName, setDisplayName] = useState(user?.displayName ?? "");
  const [password, setPassword] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (user) {
      setDisplayName(user.displayName);
    }
  }, [user]);

  const saveMutation = useMutation({
    mutationFn: () =>
      updateCurrentUser({
        displayName,
        password: password.length > 0 ? password : null,
      }),
    onSuccess: async (nextUser) => {
      auth.replaceUser(nextUser);
      setPassword("");
      setFormError(null);
      setSaved(true);
      await queryClient.invalidateQueries({ queryKey: usersQueryKey });
    },
    onError: (error) => {
      setSaved(false);
      setFormError(errorMessage(error));
    },
  });

  if (!user) {
    return null;
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaved(false);
    saveMutation.mutate();
  }

  return (
    <Panel>
      <Box component="form" onSubmit={handleSubmit}>
        <Stack spacing={2.5}>
          <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
            <ProfileAvatar>{userInitials(user)}</ProfileAvatar>
            <Stack spacing={0.5} sx={{ minWidth: 0 }}>
              <Typography variant="h6" component="h2">
                Your account
              </Typography>
              <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                <Chip size="small" label={user.role} />
                <Typography variant="body2" color="text.secondary">
                  Email and role stay as assigned.
                </Typography>
              </Stack>
            </Stack>
          </Stack>
          {formError ? <Alert severity="error">{formError}</Alert> : null}
          {saved ? <Alert severity="success">Account saved.</Alert> : null}
          <TextField label="Email" value={user.email} disabled fullWidth />
          <TextField
            label="Display name"
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            required
            fullWidth
          />
          <TextField
            label="New password"
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            helperText="Leave blank to keep the current password. New passwords need 8 or more characters, a letter, and a digit."
            fullWidth
          />
          <Stack direction="row" sx={{ justifyContent: "flex-end" }}>
            <Button type="submit" disabled={saveMutation.isPending} loading={saveMutation.isPending}>
              Save
            </Button>
          </Stack>
        </Stack>
      </Box>
    </Panel>
  );
}
