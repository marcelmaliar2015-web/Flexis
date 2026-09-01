import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Link from "@mui/material/Link";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { ProfileResumeFields } from "@/features/jobApplication/ProfileResumeFields";
import { userFacingError } from "@/shared/api/errors";
import { getGoogleConnection, googleConnectionQueryKey } from "@/shared/api/google";
import {
  getJobResumeBoard,
  jobResumeQueryKey,
  updateJobResumeOwnerOptions,
  updateJobResumeProfile,
} from "@/shared/api/resume";
import { isQueryLoading } from "@/shared/api/queryState";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3),
}));

type JobApplicationResumeTabProps = {
  actionsEnabled?: boolean;
};

export function JobApplicationResumeTab({ actionsEnabled }: JobApplicationResumeTabProps) {
  const queryClient = useQueryClient();
  const connectionQuery = useQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });
  const connected = actionsEnabled ?? connectionQuery.data?.connected === true;
  const boardQuery = useQuery({
    queryKey: jobResumeQueryKey,
    queryFn: getJobResumeBoard,
    enabled: connected,
  });
  const profiles = boardQuery.data?.profiles ?? [];
  const [profileId, setProfileId] = useState("");
  const [prompt, setPrompt] = useState("");
  const [resumeStyle, setResumeStyle] = useState<number | "">("");
  const [owner, setOwner] = useState("");
  const [ownerOptions, setOwnerOptions] = useState<string[]>([]);
  const [newOwnerOption, setNewOwnerOption] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [ownerError, setOwnerError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [ownersSaved, setOwnersSaved] = useState(false);

  useEffect(() => {
    if (profiles.length === 0) {
      setProfileId("");
      return;
    }

    if (!profiles.some((item) => item.profileId === profileId)) {
      setProfileId(profiles[0].profileId);
    }
  }, [profileId, profiles]);

  useEffect(() => {
    if (boardQuery.data) {
      setOwnerOptions(boardQuery.data.ownerOptions);
    }
  }, [boardQuery.data]);

  useEffect(() => {
    const selected = profiles.find((item) => item.profileId === profileId);
    if (!selected) {
      setPrompt("");
      setResumeStyle("");
      setOwner("");
      return;
    }

    setPrompt(selected.prompt);
    setResumeStyle(selected.resumeStyle ?? "");
    setOwner(selected.owner);
    setSaved(false);
    setFormError(null);
  }, [profileId, profiles]);

  const profileMutation = useMutation({
    mutationFn: () =>
      updateJobResumeProfile(profileId, {
        prompt,
        resumeStyle: resumeStyle === "" ? null : resumeStyle,
        owner: owner.trim().length > 0 ? owner.trim() : null,
      }),
    onSuccess: async (result) => {
      setOwnerOptions(result.ownerOptions);
      setSaved(true);
      setFormError(null);
      await queryClient.invalidateQueries({ queryKey: jobResumeQueryKey });
    },
    onError: (error) => {
      setSaved(false);
      setFormError(userFacingError(error));
    },
  });

  const ownersMutation = useMutation({
    mutationFn: () => updateJobResumeOwnerOptions({ ownerOptions }),
    onSuccess: async (result) => {
      setOwnerOptions(result.ownerOptions);
      setOwnersSaved(true);
      setOwnerError(null);
      await queryClient.invalidateQueries({ queryKey: jobResumeQueryKey });
    },
    onError: (error) => {
      setOwnersSaved(false);
      setOwnerError(userFacingError(error));
    },
  });

  const loading = isQueryLoading(boardQuery.data, boardQuery.isPending);
  const selected = profiles.find((item) => item.profileId === profileId) ?? null;

  function handleProfileSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!profileId || !connected) {
      return;
    }

    profileMutation.mutate();
  }

  function handleOwnersSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!connected) {
      return;
    }

    ownersMutation.mutate();
  }

  function addOwnerOption() {
    const trimmed = newOwnerOption.trim();
    if (trimmed.length === 0) {
      return;
    }

    if (!ownerOptions.some((item) => item.toLowerCase() === trimmed.toLowerCase())) {
      setOwnerOptions((current) => [...current, trimmed]);
    }

    setNewOwnerOption("");
    setOwnersSaved(false);
    setOwnerError(null);
  }

  function removeOwnerOption(option: string) {
    setOwnerOptions((current) => current.filter((item) => item !== option));
    if (owner === option) {
      setOwner("");
    }

    setOwnersSaved(false);
    setOwnerError(null);
  }

  return (
    <Stack spacing={4}>
      {!connected ? (
        <Alert severity="info">Connect Gmail to configure resume generation and sync job-master.</Alert>
      ) : null}
      <Panel>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              job-master sheet
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Flexis keeps a job-master workbook in your Flexis Drive folder. The Profile Management
              tab lists profiles with resume settings.
            </Typography>
          </Stack>
          {boardQuery.data?.jobMasterUrl ? (
            <Link href={boardQuery.data.jobMasterUrl} target="_blank" rel="noopener noreferrer">
              {boardQuery.data.jobMasterUrl}
            </Link>
          ) : (
            <Typography variant="body2" color="text.secondary">
              The job-master sheet URL appears here after you save resume settings.
            </Typography>
          )}
        </Stack>
      </Panel>
      <Panel>
        <Box component="form" onSubmit={handleOwnersSubmit}>
          <Stack spacing={2}>
            <Stack spacing={0.5}>
              <Typography variant="h6" component="h2">
                Owner options
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Build the owner dropdown list used on each profile. Add or remove options, then save.
              </Typography>
            </Stack>
            {ownerError ? <Alert severity="error">{ownerError}</Alert> : null}
            {ownersSaved && !ownerError ? (
              <Alert severity="success">Owner options saved and job-master updated.</Alert>
            ) : null}
            <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
              {ownerOptions.map((option) => (
                <Chip
                  key={option}
                  label={option}
                  onDelete={
                    connected && !ownersMutation.isPending
                      ? () => removeOwnerOption(option)
                      : undefined
                  }
                />
              ))}
              {ownerOptions.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  No owner options yet.
                </Typography>
              ) : null}
            </Stack>
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
              <TextField
                label="New owner option"
                value={newOwnerOption}
                onChange={(event) => {
                  setOwnersSaved(false);
                  setNewOwnerOption(event.target.value);
                }}
                disabled={!connected || ownersMutation.isPending}
                fullWidth
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    addOwnerOption();
                  }
                }}
              />
              <Button
                variant="outlined"
                disabled={!connected || ownersMutation.isPending || newOwnerOption.trim().length === 0}
                onClick={addOwnerOption}
                sx={{ flexShrink: 0 }}
              >
                Add
              </Button>
            </Stack>
            <Button
              type="submit"
              disabled={!connected || ownersMutation.isPending}
              loading={ownersMutation.isPending}
            >
              Save owner options
            </Button>
          </Stack>
        </Box>
      </Panel>
      <Panel>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Profile resume settings
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Configure prompt, resume style, and owner for each profile. Profiles with any of these
              set appear as rows on the job-master Profile Management tab.
            </Typography>
          </Stack>
          {formError ? <Alert severity="error">{formError}</Alert> : null}
          {saved && !formError ? (
            <Alert severity="success">Profile resume settings saved and job-master updated.</Alert>
          ) : null}
          {profiles.length === 0 && connected ? (
            <Alert severity="info">Create a profile on Settings first.</Alert>
          ) : null}
          <Box component="form" onSubmit={handleProfileSubmit}>
            <Stack spacing={2}>
              <TextField
                select
                label="Profile"
                value={profileId}
                onChange={(event) => {
                  setProfileId(event.target.value);
                  setSaved(false);
                  setFormError(null);
                }}
                disabled={!connected || profiles.length === 0 || profileMutation.isPending}
                fullWidth
              >
                {profiles.map((item) => (
                  <MenuItem key={item.profileId} value={item.profileId}>
                    {item.title}
                  </MenuItem>
                ))}
              </TextField>
              <ProfileResumeFields
                prompt={prompt}
                resumeStyle={resumeStyle}
                owner={owner}
                ownerOptions={ownerOptions}
                onPromptChange={(value) => {
                  setSaved(false);
                  setPrompt(value);
                }}
                onResumeStyleChange={(value) => {
                  setSaved(false);
                  setResumeStyle(value);
                }}
                onOwnerChange={(value) => {
                  setSaved(false);
                  setOwner(value);
                }}
                disabled={!connected || !profileId || loading || profileMutation.isPending}
              />
              <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                <Button
                  type="submit"
                  disabled={!connected || !profileId || loading || profileMutation.isPending}
                  loading={profileMutation.isPending}
                >
                  Save profile resume settings
                </Button>
                {selected?.url ? (
                  <Typography variant="body2" color="text.secondary">
                    Profile sheet: {selected.title}
                  </Typography>
                ) : null}
              </Stack>
            </Stack>
          </Box>
        </Stack>
      </Panel>
    </Stack>
  );
}
