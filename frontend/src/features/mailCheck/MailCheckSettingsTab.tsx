import Alert from "@mui/material/Alert";
import Autocomplete from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Checkbox from "@mui/material/Checkbox";
import FormControl from "@mui/material/FormControl";
import FormControlLabel from "@mui/material/FormControlLabel";
import FormGroup from "@mui/material/FormGroup";
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
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { MailCheckMailboxCard } from "@/features/mailCheck/MailCheckMailboxCard";
import { Panel } from "@/features/mailCheck/mailCheckLayout";
import { errorMessage } from "@/features/mailCheck/mailCheckUi";
import {
  bumpMailCheckSettingsRevision,
  getMailCheckSettings,
  listMailCheckModels,
  mailCheckModelsQueryKey,
  mailCheckNeedActionQueryKey,
  mailCheckSettingsQueryKey,
  updateMailCheckSettings,
} from "@/shared/api/mailCheck";
import {
  mailCheckLabels,
  mailCheckMailboxActions,
  type MailCheckLabelSlug,
  type MailCheckMailboxAction,
} from "@/shared/types/mailCheck";

function buildLabelActions(
  source: Record<MailCheckLabelSlug, MailCheckMailboxAction> | undefined,
  defaults?: Record<MailCheckLabelSlug, MailCheckMailboxAction>,
): Record<MailCheckLabelSlug, MailCheckMailboxAction> {
  const next = {} as Record<MailCheckLabelSlug, MailCheckMailboxAction>;
  for (const item of mailCheckLabels) {
    next[item.slug] = source?.[item.slug] ?? defaults?.[item.slug] ?? "keep";
  }
  return next;
}

function buildNeedActionLabels(source: MailCheckLabelSlug[] | undefined): MailCheckLabelSlug[] {
  if (!source || source.length === 0) {
    return ["schedule", "assessment", "availability"];
  }

  return source;
}

export function MailCheckSettingsTab() {
  const queryClient = useQueryClient();
  const settingsQuery = useQuery({
    queryKey: mailCheckSettingsQueryKey,
    queryFn: getMailCheckSettings,
  });
  const [apiKey, setApiKey] = useState("");
  const [model, setModel] = useState("gpt-4.1-mini");
  const [classifierPrompt, setClassifierPrompt] = useState("");
  const [labelActions, setLabelActions] = useState<Record<MailCheckLabelSlug, MailCheckMailboxAction>>(
    () => buildLabelActions(undefined),
  );
  const [needActionLabels, setNeedActionLabels] = useState<MailCheckLabelSlug[]>(() =>
    buildNeedActionLabels(undefined),
  );
  const [formError, setFormError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (settingsQuery.data) {
      setModel(settingsQuery.data.model);
      setClassifierPrompt(settingsQuery.data.classifierPrompt);
      setLabelActions(
        buildLabelActions(settingsQuery.data.labelActions, settingsQuery.data.defaultLabelActions),
      );
      setNeedActionLabels(buildNeedActionLabels(settingsQuery.data.needActionLabels));
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
        classifierPrompt: classifierPrompt.trim(),
        labelActions,
        needActionLabels,
      }),
    onSuccess: async (result) => {
      setApiKey("");
      setModel(result.model);
      setClassifierPrompt(result.classifierPrompt);
      setLabelActions(buildLabelActions(result.labelActions, result.defaultLabelActions));
      setNeedActionLabels(buildNeedActionLabels(result.needActionLabels));
      setFormError(null);
      setSaved(true);
      bumpMailCheckSettingsRevision(queryClient);
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckModelsQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckNeedActionQueryKey });
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
        model: model.trim() || "gpt-4.1-mini",
        classifierPrompt: classifierPrompt.trim(),
        labelActions,
        needActionLabels,
      }),
    onSuccess: async () => {
      setApiKey("");
      setFormError(null);
      setSaved(true);
      bumpMailCheckSettingsRevision(queryClient);
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckModelsQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckNeedActionQueryKey });
    },
    onError: (error) => {
      setSaved(false);
      setFormError(errorMessage(error));
    },
  });

  const autoCheckMutation = useMutation({
    mutationFn: (enabled: boolean) =>
      updateMailCheckSettings({
        apiKey: null,
        clearApiKey: false,
        model: settings?.model ?? (model.trim() || "gpt-4.1-mini"),
        autoCheckEnabled: enabled,
      }),
    onSuccess: async () => {
      setFormError(null);
      bumpMailCheckSettingsRevision(queryClient);
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
    },
    onError: (error) => {
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

    if (classifierPrompt.trim().length === 0) {
      setSaved(false);
      setFormError("Classifier prompt cannot be empty.");
      return;
    }

    if (needActionLabels.length === 0) {
      setSaved(false);
      setFormError("Choose at least one Need action label.");
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
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Auto check
            </Typography>
            <Typography variant="body2" color="text.secondary">
              When enabled, Flexis checks new mail every {settings?.autoCheckIntervalSeconds ?? 20}{" "}
              seconds while the Mail Check page is visible. Each run classifies up to three messages
              per mailbox when more mail is waiting.
            </Typography>
          </Stack>
          <FormControlLabel
            control={
              <Switch
                checked={settings?.autoCheckEnabled ?? true}
                onChange={(event) => autoCheckMutation.mutate(event.target.checked)}
                disabled={autoCheckMutation.isPending || settingsQuery.isLoading}
              />
            }
            label={settings?.autoCheckEnabled ? "Enabled" : "Disabled"}
          />
        </Stack>
      </Panel>
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
                      : "Save a key first to load models, or type an id such as gpt-4.1-mini."
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
      <Panel>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Label actions
            </Typography>
            <Typography variant="body2" color="text.secondary">
              After the classifier picks a label, Flexis applies pin, trash, or keep. Pin creates a
              mailbox label or category; Gmail also stars the message. Outlook also pins to the top of the inbox. Trash moves it to trash.
              Keep leaves the message untouched.
            </Typography>
          </Stack>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">Label</TableCell>
                  <TableCell align="left">Action</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {mailCheckLabels.map((item) => (
                  <TableRow key={item.slug}>
                    <TableCell align="left">{item.name}</TableCell>
                    <TableCell align="left">
                      <FormControl size="small" sx={{ minWidth: 140 }}>
                        <Select
                          value={labelActions[item.slug]}
                          onChange={(event) => {
                            setSaved(false);
                            setLabelActions((current) => ({
                              ...current,
                              [item.slug]: event.target.value as MailCheckMailboxAction,
                            }));
                          }}
                          disabled={saveMutation.isPending || clearMutation.isPending}
                        >
                          {mailCheckMailboxActions.map((action) => (
                            <MenuItem key={action.value} value={action.value}>
                              {action.label}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <Stack direction="row" spacing={1}>
            <Button
              disabled={saveMutation.isPending || clearMutation.isPending || settingsQuery.isLoading}
              loading={saveMutation.isPending}
              onClick={() => {
                setSaved(false);
                setLabelActions(buildLabelActions(settings?.defaultLabelActions));
              }}
            >
              Reset to defaults
            </Button>
            <Button
              disabled={saveMutation.isPending || clearMutation.isPending || settingsQuery.isLoading}
              loading={saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
            >
              Save actions
            </Button>
          </Stack>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Need action labels
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Choose which classifier labels appear on the Need action tab. Only pinned mail in your
              mailbox is shown. The tab badge counts matching messages across all connected
              mailboxes.
            </Typography>
          </Stack>
          <FormGroup>
            {mailCheckLabels.map((item) => (
              <FormControlLabel
                key={item.slug}
                control={
                  <Checkbox
                    checked={needActionLabels.includes(item.slug)}
                    onChange={(event) => {
                      setSaved(false);
                      setNeedActionLabels((current) => {
                        if (event.target.checked) {
                          return [...current, item.slug];
                        }

                        return current.filter((slug) => slug !== item.slug);
                      });
                    }}
                    disabled={saveMutation.isPending || clearMutation.isPending}
                  />
                }
                label={item.name}
              />
            ))}
          </FormGroup>
          <Stack direction="row" spacing={1}>
            <Button
              disabled={saveMutation.isPending || clearMutation.isPending || settingsQuery.isLoading}
              loading={saveMutation.isPending}
              onClick={() => {
                setSaved(false);
                setNeedActionLabels(buildNeedActionLabels(settings?.defaultNeedActionLabels));
              }}
            >
              Reset to defaults
            </Button>
            <Button
              disabled={saveMutation.isPending || clearMutation.isPending || settingsQuery.isLoading}
              loading={saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
            >
              Save Need action
            </Button>
          </Stack>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Classifier prompt
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Flexis classifies one message at a time and expects JSON with a single label field.
              Edit the prompt here to refine how messages are labeled before actions run.
            </Typography>
          </Stack>
          <TextField
            label="Prompt"
            value={classifierPrompt}
            onChange={(event) => {
              setSaved(false);
              setClassifierPrompt(event.target.value);
            }}
            multiline
            minRows={12}
            maxRows={24}
            disabled={saveMutation.isPending || clearMutation.isPending}
          />
          <Stack direction="row" spacing={1}>
            <Button
              disabled={saveMutation.isPending || clearMutation.isPending || settingsQuery.isLoading}
              loading={saveMutation.isPending}
              onClick={() => {
                setSaved(false);
                setClassifierPrompt(settings?.defaultClassifierPrompt ?? classifierPrompt);
              }}
            >
              Reset to default
            </Button>
            <Button
              disabled={saveMutation.isPending || clearMutation.isPending || settingsQuery.isLoading}
              loading={saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
            >
              Save prompt
            </Button>
          </Stack>
        </Stack>
      </Panel>
    </Stack>
  );
}
