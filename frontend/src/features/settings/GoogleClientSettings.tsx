import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { userFacingError } from "@/shared/api/errors";
import {
  getGoogleClient,
  googleClientQueryKey,
  googleConnectionQueryKey,
  saveGoogleClient,
} from "@/shared/api/google";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3),
}));

function errorMessage(error: unknown): string | null {
  return userFacingError(error);
}

export function GoogleClientSettings() {
  const queryClient = useQueryClient();
  const clientQuery = useQuery({
    queryKey: googleClientQueryKey,
    queryFn: getGoogleClient,
  });
  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (clientQuery.data) {
      setClientId(clientQuery.data.clientId);
    }
  }, [clientQuery.data]);

  const saveMutation = useMutation({
    mutationFn: () =>
      saveGoogleClient({
        clientId,
        clientSecret: clientSecret.length > 0 ? clientSecret : null,
      }),
    onSuccess: async () => {
      setClientSecret("");
      setFormError(null);
      await queryClient.invalidateQueries({ queryKey: googleClientQueryKey });
      await queryClient.invalidateQueries({ queryKey: googleConnectionQueryKey });
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    saveMutation.mutate();
  }

  return (
    <Panel>
      <Box component="form" onSubmit={handleSubmit}>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Google Cloud client
            </Typography>
            <Typography variant="body2" color="text.secondary">
              One client for Flexis. Each person still connects their own Gmail on Job Application.
              Create the web client with Help, then paste Client ID and Client secret here.
            </Typography>
          </Stack>
          {errorMessage(clientQuery.error) ? <Alert severity="error">{errorMessage(clientQuery.error)}</Alert> : null}
          {formError ? <Alert severity="error">{formError}</Alert> : null}
          {saveMutation.isSuccess ? <Alert severity="success">Google Cloud client saved.</Alert> : null}
          <TextField
            label="Client ID"
            value={clientId}
            onChange={(event) => setClientId(event.target.value)}
            required
            fullWidth
          />
          <TextField
            label={
              clientQuery.data?.hasSecret
                ? "Client secret (leave blank to keep current)"
                : "Client secret"
            }
            type="password"
            autoComplete="new-password"
            value={clientSecret}
            onChange={(event) => setClientSecret(event.target.value)}
            required={!clientQuery.data?.hasSecret}
            fullWidth
          />
          <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
            <Button type="submit" disabled={saveMutation.isPending} loading={saveMutation.isPending}>
              Save
            </Button>
          </Stack>
        </Stack>
      </Box>
    </Panel>
  );
}
