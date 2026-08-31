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
  getMicrosoftClient,
  microsoftClientQueryKey,
  saveMicrosoftClient,
} from "@/shared/api/microsoft";
import { mailCheckMailboxQueryKey } from "@/shared/api/mailCheck";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3),
}));

function errorMessage(error: unknown): string | null {
  return userFacingError(error);
}

export function MicrosoftClientSettings() {
  const queryClient = useQueryClient();
  const clientQuery = useQuery({
    queryKey: microsoftClientQueryKey,
    queryFn: getMicrosoftClient,
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
      saveMicrosoftClient({
        clientId,
        clientSecret: clientSecret.length > 0 ? clientSecret : null,
      }),
    onSuccess: async () => {
      setClientSecret("");
      setFormError(null);
      await queryClient.invalidateQueries({ queryKey: microsoftClientQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckMailboxQueryKey });
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
              Microsoft client
            </Typography>
            <Typography variant="body2" color="text.secondary">
              One Azure app for Mail Check Outlook connect. Each person still connects their own
              mailbox on Mail Check Settings. Create the app with Help, then paste Application ID
              and client secret here.
            </Typography>
          </Stack>
          {formError ? <Alert severity="error">{formError}</Alert> : null}
          {saveMutation.isSuccess ? <Alert severity="success">Microsoft client saved.</Alert> : null}
          <TextField
            label="Application (client) ID"
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
