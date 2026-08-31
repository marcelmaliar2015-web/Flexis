import Alert from "@mui/material/Alert";
import Autocomplete from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { MailCheckMailboxCard } from "@/features/mailCheck/MailCheckMailboxCard";
import { Panel } from "@/features/mailCheck/mailCheckLayout";
import { errorMessage } from "@/features/mailCheck/mailCheckUi";
import {
  getMailCheckSettings,
  listMailCheckModels,
  mailCheckModelsQueryKey,
  mailCheckSettingsQueryKey,
  updateMailCheckSettings,
} from "@/shared/api/mailCheck";

export function MailCheckSettingsTab() {
  const queryClient = useQueryClient();
  const settingsQuery = useQuery({
    queryKey: mailCheckSettingsQueryKey,
    queryFn: getMailCheckSettings,
  });
  const [apiKey, setApiKey] = useState("");
  const [model, setModel] = useState("gpt-4o-mini");
  const [formError, setFormError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (settingsQuery.data) {
      setModel(settingsQuery.data.model);
    }
  }, [settingsQuery.data]);

  const modelsQuery = useQuery({
    queryKey: mailCheckModelsQueryKey,
    queryFn: listMailCheckModels,
    enabled: settingsQuery.data?.hasApiKey === true,
  });

  const saveMutation = useMutation({
    mutationFn: () =>
      updateMailCheckSettings({
        apiKey: apiKey.trim().length > 0 ? apiKey.trim() : null,
        clearApiKey: false,
        model: model.trim(),
      }),
    onSuccess: async (result) => {
      setApiKey("");
      setModel(result.model);
      setFormError(null);
      setSaved(true);
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckModelsQueryKey });
    },
    onError: (error) => {
      setSaved(false);
      setFormError(errorMessage(error));
    },
  });

  const clearMutation = useMutation({
    mutationFn: () =>
      updateMailCheckSettings({
        apiKey: null,
        clearApiKey: true,
        model: model.trim() || "gpt-4o-mini",
      }),
    onSuccess: async () => {
      setApiKey("");
      setFormError(null);
      setSaved(true);
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckModelsQueryKey });
    },
    onError: (error) => {
      setSaved(false);
      setFormError(errorMessage(error));
    },
  });

  const settings = settingsQuery.data;
  const listed = modelsQuery.data?.models.map((item) => item.id) ?? [];
  const modelOptions =
    model.length > 0 && !listed.includes(model) ? [model, ...listed] : listed;

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (model.trim().length === 0) {
      setSaved(false);
      setFormError("Choose a model.");
      return;
    }

    saveMutation.mutate();
  }

  return (
    <Stack spacing={2}>
      {formError ? <Alert severity="error">{formError}</Alert> : null}
      {saved && !formError ? <Alert severity="success">Mail Check settings saved.</Alert> : null}
      <MailCheckMailboxCard />
      <Panel>
        <Box component="form" onSubmit={handleSubmit}>
          <Stack spacing={2}>
            <Stack spacing={0.5}>
              <Typography variant="h6" component="h2">
                OpenAI
              </Typography>
              <Typography variant="body2" color="text.secondary">
                The API key is the only paid piece. Flexis stores it protected and never shows it
                again. Pick any chat or reasoning model from your account. Usage differs by model;
                Flexis adapts token limits and request shape automatically.
              </Typography>
            </Stack>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
              {settings?.hasApiKey ? (
                <Chip color="success" label="Key saved" />
              ) : (
                <Chip label="No key" />
              )}
            </Stack>
            <TextField
              label="API key"
              type="password"
              value={apiKey}
              autoComplete="off"
              onChange={(event) => {
                setSaved(false);
                setApiKey(event.target.value);
              }}
              placeholder={settings?.hasApiKey ? "Leave blank to keep the saved key" : "sk-..."}
              disabled={saveMutation.isPending || clearMutation.isPending}
            />
            <Autocomplete
              freeSolo
              options={modelOptions}
              value={model}
              onChange={(_event, value) => {
                setSaved(false);
                setModel(value ?? "");
              }}
              onInputChange={(_event, value) => {
                setSaved(false);
                setModel(value);
              }}
              disabled={saveMutation.isPending || clearMutation.isPending}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Model"
                  helperText={
                    settings?.hasApiKey
                      ? "Recommended models are listed first. You can type any id."
                      : "Save a key first to load models, or type an id such as gpt-4o-mini."
                  }
                />
              )}
            />
            <Stack direction="row" spacing={1}>
              <Button
                type="submit"
                disabled={saveMutation.isPending || clearMutation.isPending || settingsQuery.isLoading}
                loading={saveMutation.isPending}
              >
                Save
              </Button>
              {settings?.hasApiKey ? (
                <Button
                  variant="outlined"
                  disabled={saveMutation.isPending || clearMutation.isPending}
                  loading={clearMutation.isPending}
                  onClick={() => clearMutation.mutate()}
                >
                  Remove key
                </Button>
              ) : null}
            </Stack>
          </Stack>
        </Box>
      </Panel>
    </Stack>
  );
}
