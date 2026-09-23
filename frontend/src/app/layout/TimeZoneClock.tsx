import Autocomplete from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Divider from "@mui/material/Divider";
import Popover from "@mui/material/Popover";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation } from "@tanstack/react-query";
import { useMemo, useState, type MouseEvent, type SyntheticEvent } from "react";
import { updateCurrentUser } from "@/shared/api/auth";
import { userFacingError } from "@/shared/api/errors";
import { useAuth } from "@/shared/auth/AuthProvider";
import {
  formatClockTime,
  formatClockTimeShort,
  formatFullDate,
} from "@/shared/time/formatTime";
import { useNow } from "@/shared/time/useNow";
import { useTimeZone } from "@/shared/time/useTimeZone";
import {
  browserTimeZoneId,
  formatTimeZoneAbbr,
  formatTimeZoneCity,
  formatTimeZoneLabel,
  formatTimeZoneOffset,
  listTimeZoneOptions,
  type TimeZoneOption,
} from "@/shared/time/timeZones";

const ClockTrigger = styled(Button)(({ theme }) => ({
  minHeight: 38,
  minWidth: 0,
  paddingLeft: theme.spacing(1.5),
  paddingRight: theme.spacing(1.5),
  borderRadius: 999,
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  color: theme.palette.text.primary,
  boxShadow: "none",
  textTransform: "none",
  "&:hover": {
    backgroundColor: theme.palette.action.hover,
    borderColor: theme.palette.primary.light,
    boxShadow: "none",
  },
}));

const ClockPanel = styled(Box)(({ theme }) => ({
  width: 360,
  maxWidth: "calc(100vw - 24px)",
  padding: theme.spacing(2),
}));

const HeroTime = styled(Typography)(({ theme }) => ({
  fontFamily: theme.typography.h3.fontFamily,
  fontWeight: 600,
  letterSpacing: "-0.03em",
  lineHeight: 1.1,
  fontVariantNumeric: "tabular-nums",
}));

const TriggerTime = styled("span")({
  fontVariantNumeric: "tabular-nums",
  fontWeight: 600,
  lineHeight: 1.15,
});

const TriggerMeta = styled("span")(({ theme }) => ({
  color: theme.palette.text.secondary,
  fontSize: "0.7rem",
  lineHeight: 1.2,
  letterSpacing: "0.02em",
}));

const QUICK_ZONE_IDS = [
  "UTC",
  "America/New_York",
  "America/Chicago",
  "America/Denver",
  "America/Los_Angeles",
  "Europe/London",
] as const;

export function TimeZoneClock() {
  const auth = useAuth();
  const user = auth.user;
  const timeZone = useTimeZone();
  const now = useNow(1000);
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const [draftZone, setDraftZone] = useState(timeZone);
  const [error, setError] = useState<string | null>(null);
  const options = useMemo(() => listTimeZoneOptions(), []);
  const open = Boolean(anchor);

  const saveMutation = useMutation({
    mutationFn: (nextZone: string) =>
      updateCurrentUser({
        displayName: user?.displayName ?? "",
        password: null,
        timeZoneId: nextZone,
      }),
    onSuccess: (nextUser) => {
      auth.replaceUser(nextUser);
      setError(null);
      setDraftZone(resolveSavedZone(nextUser.timeZoneId));
    },
    onError: (saveError) => {
      setError(userFacingError(saveError) ?? "Could not save time zone.");
    },
  });

  if (!user) {
    return null;
  }

  const displayZone = open ? draftZone : timeZone;
  const abbr = formatTimeZoneAbbr(displayZone, now);
  const city = formatTimeZoneCity(displayZone);
  const selectedOption =
    options.find((option) => option.id === draftZone) ??
    ({
      id: draftZone,
      label: formatTimeZoneLabel(draftZone, now),
      group: "All",
    } satisfies TimeZoneOption);

  function openPanel(event: MouseEvent<HTMLElement>) {
    setDraftZone(timeZone);
    setError(null);
    setAnchor(event.currentTarget);
  }

  function closePanel() {
    setAnchor(null);
    setError(null);
    setDraftZone(timeZone);
  }

  function applyZone(nextZone: string) {
    if (nextZone === resolveSavedZone(user.timeZoneId) && user.timeZoneId.trim().length > 0) {
      setDraftZone(nextZone);
      return;
    }

    setDraftZone(nextZone);
    saveMutation.mutate(nextZone);
  }

  function handleSelect(_event: SyntheticEvent, value: TimeZoneOption | null) {
    if (!value) {
      return;
    }

    applyZone(value.id);
  }

  return (
    <>
      <ClockTrigger
        onClick={openPanel}
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-label={`Current time ${formatClockTimeShort(now, timeZone)} ${abbr}. Change time zone.`}
      >
        <Stack spacing={0.15} sx={{ alignItems: "flex-end", minWidth: 0 }}>
          <TriggerTime>{formatClockTimeShort(now, timeZone)}</TriggerTime>
          <TriggerMeta>
            {abbr} · {city}
          </TriggerMeta>
        </Stack>
      </ClockTrigger>
      <Popover
        open={open}
        anchorEl={anchor}
        onClose={closePanel}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
        slotProps={{
          paper: {
            elevation: 0,
            sx: (theme) => ({
              mt: 1,
              border: `1px solid ${theme.palette.divider}`,
              boxShadow: "0 16px 40px rgba(14, 39, 68, 0.12)",
              borderRadius: 2,
            }),
          },
        }}
      >
        <ClockPanel>
          <Stack spacing={2}>
            <Stack spacing={0.5}>
              <Typography variant="overline" color="text.secondary">
                Workspace time
              </Typography>
              <HeroTime variant="h3">{formatClockTime(now, displayZone)}</HeroTime>
              <Typography variant="body2" color="text.secondary">
                {formatFullDate(now, displayZone)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {formatTimeZoneLabel(displayZone, now)} · {formatTimeZoneOffset(displayZone, now)}
              </Typography>
            </Stack>

            <Divider />

            <Stack spacing={1}>
              <Typography variant="subtitle2">Time zone</Typography>
              <Autocomplete
                options={options}
                groupBy={(option) => option.group}
                value={selectedOption}
                onChange={handleSelect}
                getOptionLabel={(option) => option.label}
                isOptionEqualToValue={(option, value) => option.id === value.id}
                disableClearable
                fullWidth
                size="small"
                renderInput={(params) => (
                  <TextField
                    {...params}
                    label="Search cities or regions"
                    placeholder="Denver, London, Tokyo…"
                  />
                )}
              />
              <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
                <Chip
                  size="small"
                  label="This device"
                  variant={draftZone === browserTimeZoneId() ? "filled" : "outlined"}
                  color={draftZone === browserTimeZoneId() ? "primary" : "default"}
                  onClick={() => applyZone(browserTimeZoneId())}
                  disabled={saveMutation.isPending}
                />
                {QUICK_ZONE_IDS.map((zoneId) => (
                  <Chip
                    key={zoneId}
                    size="small"
                    label={formatTimeZoneCity(zoneId)}
                    variant={draftZone === zoneId ? "filled" : "outlined"}
                    color={draftZone === zoneId ? "primary" : "default"}
                    onClick={() => applyZone(zoneId)}
                    disabled={saveMutation.isPending}
                  />
                ))}
              </Stack>
            </Stack>

            {error ? (
              <Typography variant="body2" color="error">
                {error}
              </Typography>
            ) : null}
            {saveMutation.isPending ? (
              <Typography variant="body2" color="text.secondary">
                Saving…
              </Typography>
            ) : null}
            {saveMutation.isSuccess && !saveMutation.isPending && !error ? (
              <Typography variant="body2" color="success.main">
                Saved. All Flexis timestamps use this zone.
              </Typography>
            ) : (
              <Typography variant="caption" color="text.secondary">
                Logs, charts, and activity stamps follow this zone. You can also change it in Settings →
                Account.
              </Typography>
            )}
          </Stack>
        </ClockPanel>
      </Popover>
    </>
  );
}

function resolveSavedZone(timeZoneId: string): string {
  const trimmed = timeZoneId.trim();
  return trimmed.length > 0 ? trimmed : browserTimeZoneId();
}
