import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { userFacingError } from "@/shared/api/errors";
import {
  getProfileInfo,
  listJobCatalogItems,
  jobCatalogQueryKey,
  profileInfoQueryKey,
  updateProfileInfo,
} from "@/shared/api/jobCatalog";
import { isQueryLoading } from "@/shared/api/queryState";
import { emptyProfileInfo, type ProfileInfo } from "@/shared/types/jobCatalog";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3),
}));

const FieldGrid = styled(Box)(({ theme }) => ({
  display: "grid",
  gap: theme.spacing(2),
  gridTemplateColumns: "1fr",
  [theme.breakpoints.up("md")]: {
    gridTemplateColumns: "1fr 1fr",
  },
}));

type ProfileInfoPanelProps = {
  actionsEnabled: boolean;
  profileId?: string;
};

export function ProfileInfoPanel({ actionsEnabled, profileId: fixedProfileId }: ProfileInfoPanelProps) {
  const queryClient = useQueryClient();
  const profilesQuery = useQuery({
    queryKey: jobCatalogQueryKey("profiles"),
    queryFn: () => listJobCatalogItems("profiles"),
  });
  const profiles = profilesQuery.data ?? [];
  const [profileId, setProfileId] = useState(fixedProfileId ?? "");
  const [form, setForm] = useState<ProfileInfo>(emptyProfileInfo());
  const [formError, setFormError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (fixedProfileId) {
      setProfileId(fixedProfileId);
      return;
    }

    if (profiles.length === 0) {
      setProfileId("");
      return;
    }

    if (!profiles.some((item) => item.id === profileId)) {
      setProfileId(profiles[0].id);
    }
  }, [fixedProfileId, profileId, profiles]);

  const infoQuery = useQuery({
    queryKey: profileInfoQueryKey(profileId),
    queryFn: () => getProfileInfo(profileId),
    enabled: actionsEnabled && profileId.length > 0,
  });

  useEffect(() => {
    if (infoQuery.data) {
      setForm(infoQuery.data);
      setSaved(false);
      setFormError(null);
    }
  }, [infoQuery.data]);

  const saveMutation = useMutation({
    mutationFn: () => updateProfileInfo(profileId, form),
    onSuccess: async (result) => {
      setForm(result);
      setSaved(true);
      setFormError(null);
      await queryClient.invalidateQueries({ queryKey: profileInfoQueryKey(profileId) });
    },
    onError: (error) => {
      setSaved(false);
      setFormError(userFacingError(error));
    },
  });

  const selected = profiles.find((item) => item.id === profileId) ?? null;
  const loading =
    isQueryLoading(profilesQuery.data, profilesQuery.isPending)
    || (profileId.length > 0 && isQueryLoading(infoQuery.data, infoQuery.isPending));

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!profileId || !actionsEnabled) {
      return;
    }

    saveMutation.mutate();
  }

  function setField<K extends keyof ProfileInfo>(key: K, value: ProfileInfo[K]) {
    setSaved(false);
    setForm((current) => ({ ...current, [key]: value }));
  }

  return (
    <Panel>
      <Stack spacing={2}>
        <Stack spacing={0.5}>
          <Typography variant="h6" component="h2">
            Profile info
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Optional personal details for a profile. Flexis writes them to a locked Profile tab in
            that profile Google Sheet. Invited editors can view the tab but cannot edit it.
          </Typography>
        </Stack>
        {!actionsEnabled ? (
          <Alert severity="info">Connect Gmail to edit profile info.</Alert>
        ) : null}
        {formError ? <Alert severity="error">{formError}</Alert> : null}
        {saved && !formError ? <Alert severity="success">Profile info saved to the Profile tab.</Alert> : null}
        {!fixedProfileId && profiles.length === 0 && actionsEnabled ? (
          <Alert severity="info">Create a profile first, then fill in profile info here.</Alert>
        ) : null}
        <Box component="form" onSubmit={handleSubmit}>
          <Stack spacing={2}>
            {!fixedProfileId ? (
              <TextField
                select
                label="Profile"
                value={profileId}
                onChange={(event) => {
                  setProfileId(event.target.value);
                  setSaved(false);
                  setFormError(null);
                }}
                disabled={!actionsEnabled || profiles.length === 0 || saveMutation.isPending}
                fullWidth
              >
                {profiles.map((item) => (
                  <MenuItem key={item.id} value={item.id}>
                    {item.title}
                  </MenuItem>
                ))}
              </TextField>
            ) : null}
            <FieldGrid>
              <TextField
                label="Name"
                value={form.name}
                onChange={(event) => setField("name", event.target.value)}
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                fullWidth
              />
              <TextField
                label="Mail"
                value={form.mail}
                onChange={(event) => setField("mail", event.target.value)}
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                fullWidth
              />
              <TextField
                label="Phone"
                value={form.phone}
                onChange={(event) => setField("phone", event.target.value)}
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                fullWidth
              />
              <TextField
                label="Password"
                type="password"
                value={form.password}
                onChange={(event) => setField("password", event.target.value)}
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                fullWidth
                autoComplete="off"
              />
              <TextField
                label="LinkedIn"
                value={form.linkedIn}
                onChange={(event) => setField("linkedIn", event.target.value)}
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                fullWidth
              />
              <TextField
                label="Target Rate (Monthly)"
                value={form.targetRateMonthly}
                onChange={(event) => setField("targetRateMonthly", event.target.value)}
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                fullWidth
              />
              <TextField
                label="Sex"
                value={form.sex}
                onChange={(event) => setField("sex", event.target.value)}
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                fullWidth
              />
              <TextField
                label="Race"
                value={form.race}
                onChange={(event) => setField("race", event.target.value)}
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                fullWidth
              />
              <TextField
                label="Veteran Status"
                value={form.veteranStatus}
                onChange={(event) => setField("veteranStatus", event.target.value)}
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                fullWidth
              />
              <TextField
                label="Address"
                value={form.address}
                onChange={(event) => setField("address", event.target.value)}
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                fullWidth
                multiline
                minRows={2}
                sx={{ gridColumn: { md: "1 / -1" } }}
              />
            </FieldGrid>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
              <Button
                type="submit"
                disabled={!actionsEnabled || !profileId || loading || saveMutation.isPending}
                loading={saveMutation.isPending}
              >
                Save profile info
              </Button>
              {selected?.url ? (
                <Typography variant="body2" color="text.secondary">
                  Updates the Profile tab in the selected sheet.
                </Typography>
              ) : null}
            </Stack>
          </Stack>
        </Box>
      </Stack>
    </Panel>
  );
}
