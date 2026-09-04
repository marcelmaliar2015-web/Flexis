import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { formatCount, formatPrice } from "@/features/jobApplication/financialUi";
import { StatisticsChart } from "@/features/jobApplication/StatisticsChart";
import {
  filterStatisticsHistory,
  rollupStatisticsPoints,
  type StatisticsRangeMode,
} from "@/features/jobApplication/statisticsPeriod";
import { StatisticsPeriodTable, StatisticsTodayTable } from "@/features/jobApplication/statisticsTables";
import { getJobStatisticsBoard, jobStatisticsQueryKey } from "@/shared/api/financial";
import { isQueryLoading } from "@/shared/api/queryState";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2.5),
}));

const MetricCard = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2, 2.5),
  minWidth: 0,
  flex: 1,
}));

const FilterRow = styled(Stack)(({ theme }) => ({
  flexWrap: "wrap",
  gap: theme.spacing(1),
}));

export function JobApplicationStatisticsTab() {
  const boardQuery = useQuery({
    queryKey: jobStatisticsQueryKey,
    queryFn: getJobStatisticsBoard,
  });
  const [range, setRange] = useState<StatisticsRangeMode>("today");
  const [profileId, setProfileId] = useState<string | "all">("all");
  const board = boardQuery.data;
  const loading = isQueryLoading(boardQuery.data, boardQuery.isPending);
  const refreshing = boardQuery.isFetching && !loading;
  const profiles = board?.profiles ?? [];
  const history = board?.history ?? [];

  const periodRows = useMemo(() => {
    if (range === "today") {
      return [];
    }

    return rollupStatisticsPoints(filterStatisticsHistory(history, profileId), range);
  }, [history, profileId, range]);

  const selectedProfiles = useMemo(() => {
    if (profileId === "all") {
      return profiles;
    }

    return profiles.filter((item) => item.profileId === profileId);
  }, [profileId, profiles]);

  const summary = useMemo(() => {
    if (range === "today") {
      return {
        applied: selectedProfiles.reduce((sum, item) => sum + item.todayApplied, 0),
        interviews: selectedProfiles.reduce((sum, item) => sum + item.todayInterviews, 0),
        unapplied: selectedProfiles.reduce((sum, item) => sum + item.todayUnapplied, 0),
        ready: selectedProfiles.reduce((sum, item) => sum + item.todayReady, 0),
        notReady: selectedProfiles.reduce((sum, item) => sum + item.todayNotReady, 0),
        total: selectedProfiles.reduce((sum, item) => sum + item.todayTotal, 0),
        price: selectedProfiles.reduce((sum, item) => sum + item.todayPrice, 0),
      };
    }

    return {
      applied: periodRows.reduce((sum, row) => sum + row.applied, 0),
      interviews: periodRows.reduce((sum, row) => sum + row.interviews, 0),
      unapplied: periodRows.at(-1)?.unapplied ?? 0,
      ready: 0,
      notReady: 0,
      total: periodRows.reduce((sum, row) => sum + row.total, 0),
      price: periodRows.reduce((sum, row) => sum + row.price, 0),
    };
  }, [periodRows, range, selectedProfiles]);

  return (
    <Stack spacing={2.5}>
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ alignItems: { md: "flex-start" }, justifyContent: "space-between" }}
      >
        <Stack spacing={0.5} sx={{ minWidth: 0 }}>
          <Typography variant="h6" component="h2">
            Statistics
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Ready rows have a Download value. Applied, Interview, and blank Status count only among
            ready rows. Period charts show Applied, Interview, and Price
            {profileId === "all" ? " summed across profiles" : " for the selected profile"}.
          </Typography>
        </Stack>
        <Button
          variant="text"
          onClick={() => void boardQuery.refetch()}
          disabled={boardQuery.isFetching}
          sx={{ alignSelf: { xs: "stretch", md: "flex-start" }, flexShrink: 0 }}
        >
          {refreshing ? "Refreshing…" : "Refresh"}
        </Button>
      </Stack>

      <ToggleButtonGroup
        exclusive
        size="small"
        value={range}
        onChange={(_event, value: StatisticsRangeMode | null) => {
          if (value) {
            setRange(value);
          }
        }}
      >
        <ToggleButton value="today">Today</ToggleButton>
        <ToggleButton value="hourly">Hourly</ToggleButton>
        <ToggleButton value="daily">Daily</ToggleButton>
        <ToggleButton value="weekly">Weekly</ToggleButton>
        <ToggleButton value="monthly">Monthly</ToggleButton>
      </ToggleButtonGroup>

      {profiles.length > 1 ? (
        <FilterRow direction="row">
          <Chip
            label="All profiles"
            color={profileId === "all" ? "primary" : "default"}
            variant={profileId === "all" ? "filled" : "outlined"}
            onClick={() => setProfileId("all")}
          />
          {profiles.map((profile) => (
            <Chip
              key={profile.profileId}
              label={profile.profileTitle}
              color={profileId === profile.profileId ? "primary" : "default"}
              variant={profileId === profile.profileId ? "filled" : "outlined"}
              onClick={() => setProfileId(profile.profileId)}
            />
          ))}
        </FilterRow>
      ) : null}

      <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} useFlexGap>
        {range === "today" ? (
          <>
            <MetricCard>
              <Typography variant="caption" color="text.secondary">
                Ready
              </Typography>
              <Typography variant="h5">{loading ? "…" : formatCount(summary.ready)}</Typography>
            </MetricCard>
            <MetricCard>
              <Typography variant="caption" color="text.secondary">
                Not ready
              </Typography>
              <Typography variant="h5">{loading ? "…" : formatCount(summary.notReady)}</Typography>
            </MetricCard>
          </>
        ) : null}
        <MetricCard>
          <Typography variant="caption" color="text.secondary">
            Applied
          </Typography>
          <Typography variant="h5">{loading ? "…" : formatCount(summary.applied)}</Typography>
        </MetricCard>
        <MetricCard>
          <Typography variant="caption" color="text.secondary">
            Unapplied (blank status)
          </Typography>
          <Typography variant="h5">{loading ? "…" : formatCount(summary.unapplied)}</Typography>
        </MetricCard>
        <MetricCard>
          <Typography variant="caption" color="text.secondary">
            Interviews
          </Typography>
          <Typography variant="h5">{loading ? "…" : formatCount(summary.interviews)}</Typography>
        </MetricCard>
        <MetricCard>
          <Typography variant="caption" color="text.secondary">
            Price
          </Typography>
          <Typography variant="h5">{loading ? "…" : formatPrice(summary.price)}</Typography>
        </MetricCard>
      </Stack>

      {range === "today" ? (
        <Panel>
          <Stack spacing={1.5}>
            <Typography variant="subtitle1">Today by profile</Typography>
            <Typography variant="body2" color="text.secondary">
              Ready means Download is filled. Applied and Unapplied are among ready rows only.
            </Typography>
            {loading ? (
              <Typography variant="body2" color="text.secondary">
                Loading…
              </Typography>
            ) : (
              <StatisticsTodayTable profiles={selectedProfiles} />
            )}
          </Stack>
        </Panel>
      ) : (
        <>
          <Panel>
            <Stack spacing={2}>
              <Typography variant="subtitle1">
                Trend · Applied, Interview, Price
                {profileId === "all" ? " (all profiles)" : ""}
              </Typography>
              {loading ? (
                <Typography variant="body2" color="text.secondary">
                  Loading…
                </Typography>
              ) : (
                <StatisticsChart rows={periodRows} />
              )}
            </Stack>
          </Panel>
          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="subtitle1">Period detail</Typography>
              <StatisticsPeriodTable rows={periodRows} />
            </Stack>
          </Panel>
        </>
      )}
    </Stack>
  );
}
