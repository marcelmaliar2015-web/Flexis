import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import { styled, useTheme } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { formatCount, formatPrice } from "@/features/jobApplication/financialUi";
import { getJobStatisticsBoard, jobStatisticsQueryKey } from "@/shared/api/financial";
import { isQueryLoading } from "@/shared/api/queryState";
import type { JobStatisticsPoint, JobStatisticsProfile } from "@/shared/types/jobApplication";

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

const ChartFrame = styled(Box)({
  position: "relative",
  width: "100%",
  height: 300,
});

const ChartSvg = styled("svg")({
  display: "block",
  width: "100%",
  height: "100%",
});

const FilterRow = styled(Stack)(({ theme }) => ({
  flexWrap: "wrap",
  gap: theme.spacing(1),
}));

type RangeMode = "today" | "hourly" | "daily" | "weekly" | "monthly";

type MetricKey = "applied" | "price" | "unapplied";

type PeriodRow = {
  key: string;
  label: string;
  applied: number;
  interviews: number;
  unapplied: number;
  total: number;
  price: number;
};

function startOfIsoWeek(day: Date): string {
  const utc = new Date(Date.UTC(day.getUTCFullYear(), day.getUTCMonth(), day.getUTCDate()));
  const weekday = utc.getUTCDay() || 7;
  utc.setUTCDate(utc.getUTCDate() - weekday + 1);
  return utc.toISOString().slice(0, 10);
}

function monthKey(isoDay: string): string {
  return isoDay.slice(0, 7);
}

function dayKey(point: JobStatisticsPoint): string {
  return (point.capturedOn || point.capturedHour).slice(0, 10);
}

function formatHourLabel(value: string): string {
  const parsed = Date.parse(value);
  if (Number.isNaN(parsed)) {
    return value;
  }

  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
  }).format(new Date(parsed));
}

function formatDayLabel(day: string): string {
  const parsed = Date.parse(`${day}T12:00:00Z`);
  if (Number.isNaN(parsed)) {
    return day;
  }

  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
  }).format(new Date(parsed));
}

function formatWeekLabel(weekStart: string): string {
  return `Week of ${formatDayLabel(weekStart)}`;
}

function formatMonthLabel(month: string): string {
  const parsed = Date.parse(`${month}-01T12:00:00Z`);
  if (Number.isNaN(parsed)) {
    return month;
  }

  return new Intl.DateTimeFormat(undefined, {
    month: "long",
    year: "numeric",
  }).format(new Date(parsed));
}

function filterHistory(
  history: JobStatisticsPoint[],
  profileId: string | "all",
): JobStatisticsPoint[] {
  if (profileId === "all") {
    return history;
  }

  return history.filter((item) => item.profileId === profileId);
}

function rollupPoints(points: JobStatisticsPoint[], mode: Exclude<RangeMode, "today">): PeriodRow[] {
  const latestByBucketProfile = new Map<string, JobStatisticsPoint>();
  for (const point of points) {
    const day = dayKey(point);
    let period = day;
    if (mode === "hourly") {
      period = point.capturedHour;
    } else if (mode === "weekly") {
      period = startOfIsoWeek(new Date(`${day}T12:00:00Z`));
    } else if (mode === "monthly") {
      period = monthKey(day);
    }

    const mapKey = `${period}::${point.profileId}`;
    const existing = latestByBucketProfile.get(mapKey);
    if (!existing || Date.parse(point.capturedHour) >= Date.parse(existing.capturedHour)) {
      latestByBucketProfile.set(mapKey, point);
    }
  }

  const byPeriod = new Map<string, PeriodRow>();
  for (const [mapKey, point] of latestByBucketProfile) {
    const period = mapKey.slice(0, mapKey.indexOf("::"));
    const current = byPeriod.get(period);
    if (current) {
      current.applied += point.applied;
      current.interviews += point.interviews;
      current.unapplied += point.unapplied;
      current.total += point.total;
      current.price += point.price;
      continue;
    }

    let label = period;
    if (mode === "hourly") {
      label = formatHourLabel(period);
    } else if (mode === "daily") {
      label = formatDayLabel(period);
    } else if (mode === "weekly") {
      label = formatWeekLabel(period);
    } else {
      label = formatMonthLabel(period);
    }

    byPeriod.set(period, {
      key: period,
      label,
      applied: point.applied,
      interviews: point.interviews,
      unapplied: point.unapplied,
      total: point.total,
      price: point.price,
    });
  }

  return [...byPeriod.values()].sort((left, right) => left.key.localeCompare(right.key));
}

function metricValue(row: PeriodRow, metric: MetricKey): number {
  if (metric === "price") {
    return row.price;
  }
  if (metric === "unapplied") {
    return row.unapplied;
  }
  return row.applied;
}

function StatisticsChart({ rows, metric }: { rows: PeriodRow[]; metric: MetricKey }) {
  const theme = useTheme();
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);
  const width = 760;
  const height = 300;
  const pad = { top: 24, right: 20, bottom: 40, left: 52 };
  const values = rows.map((row) => metricValue(row, metric));
  const max = Math.max(...values, 1);
  const plotWidth = width - pad.left - pad.right;
  const plotHeight = height - pad.top - pad.bottom;
  const color =
    metric === "unapplied"
      ? theme.palette.warning.main
      : metric === "price"
        ? theme.palette.secondary.main
        : theme.palette.primary.main;

  if (rows.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        No history yet. Open Statistics or Financial while Gmail can read sheets to start capturing
        hourly points.
      </Typography>
    );
  }

  const points = rows.map((row, index) => {
    const x = pad.left + (rows.length === 1 ? plotWidth / 2 : (plotWidth * index) / (rows.length - 1));
    const y = pad.top + plotHeight - (metricValue(row, metric) / max) * plotHeight;
    return { x, y, row };
  });
  const path = points
    .map((point, index) => `${index === 0 ? "M" : "L"} ${point.x.toFixed(1)} ${point.y.toFixed(1)}`)
    .join(" ");
  const hover = hoverIndex === null ? null : points[hoverIndex];

  return (
    <ChartFrame>
      <ChartSvg viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Statistics chart">
        {[0, 0.25, 0.5, 0.75, 1].map((share) => {
          const y = pad.top + plotHeight - share * plotHeight;
          return (
            <g key={share}>
              <line
                x1={pad.left}
                x2={width - pad.right}
                y1={y}
                y2={y}
                stroke={theme.palette.divider}
                strokeWidth={1}
              />
              <text
                x={pad.left - 8}
                y={y + 4}
                textAnchor="end"
                fill={theme.palette.text.secondary}
                fontSize={11}
              >
                {metric === "price" ? formatPrice(max * share) : formatCount(Math.round(max * share))}
              </text>
            </g>
          );
        })}
        <path d={path} fill="none" stroke={color} strokeWidth={2.5} strokeLinejoin="round" />
        {points.map((point, index) => (
          <circle
            key={point.row.key}
            cx={point.x}
            cy={point.y}
            r={hoverIndex === index ? 5 : 3.5}
            fill={color}
            onMouseEnter={() => setHoverIndex(index)}
            onMouseLeave={() => setHoverIndex(null)}
          />
        ))}
        {rows.map((row, index) => {
          const x =
            pad.left + (rows.length === 1 ? plotWidth / 2 : (plotWidth * index) / (rows.length - 1));
          return (
            <rect
              key={`hit-${row.key}`}
              x={x - plotWidth / Math.max(rows.length * 2, 2)}
              y={pad.top}
              width={Math.max(plotWidth / Math.max(rows.length, 1), 24)}
              height={plotHeight}
              fill="transparent"
              onMouseEnter={() => setHoverIndex(index)}
              onMouseLeave={() => setHoverIndex(null)}
            />
          );
        })}
        {points.length <= 12
          ? points.map((point) => (
              <text
                key={`label-${point.row.key}`}
                x={point.x}
                y={height - 12}
                textAnchor="middle"
                fill={theme.palette.text.secondary}
                fontSize={10}
              >
                {point.row.label}
              </text>
            ))
          : null}
      </ChartSvg>
      {hover ? (
        <Box
          sx={{
            position: "absolute",
            left: Math.min(hover.x + 8, width - 200),
            top: Math.max(hover.y - 72, 8),
            border: 1,
            borderColor: "divider",
            borderRadius: 1,
            bgcolor: "background.paper",
            boxShadow: "0 10px 28px rgba(14, 39, 68, 0.12)",
            px: 1.5,
            py: 1.25,
            pointerEvents: "none",
            minWidth: 180,
          }}
        >
          <Typography variant="caption" color="text.secondary">
            {hover.row.label}
          </Typography>
          <Typography variant="body2">{formatCount(hover.row.applied)} applied</Typography>
          <Typography variant="body2">{formatCount(hover.row.unapplied)} unapplied</Typography>
          <Typography variant="body2">{formatPrice(hover.row.price)} price</Typography>
        </Box>
      ) : null}
    </ChartFrame>
  );
}

function TodayTable({ profiles }: { profiles: JobStatisticsProfile[] }) {
  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell align="left">Profile</TableCell>
            <TableCell align="left">Applied</TableCell>
            <TableCell align="left">Interviews</TableCell>
            <TableCell align="left">Unapplied</TableCell>
            <TableCell align="left">Listings</TableCell>
            <TableCell align="left">Price</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {profiles.length === 0 ? (
            <TableRow>
              <TableCell colSpan={6} align="left">
                <Typography variant="body2" color="text.secondary">
                  Add pipeline rows on Operations to track profile statistics.
                </Typography>
              </TableCell>
            </TableRow>
          ) : (
            profiles.map((profile) => (
              <TableRow key={profile.profileId} hover>
                <TableCell align="left">{profile.profileTitle}</TableCell>
                <TableCell align="left">{formatCount(profile.applied)}</TableCell>
                <TableCell align="left">{formatCount(profile.interviews)}</TableCell>
                <TableCell align="left">{formatCount(profile.unapplied)}</TableCell>
                <TableCell align="left">{formatCount(profile.total)}</TableCell>
                <TableCell align="left">{formatPrice(profile.price)}</TableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

function PeriodTable({ rows }: { rows: PeriodRow[] }) {
  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell align="left">Period</TableCell>
            <TableCell align="left">Applied</TableCell>
            <TableCell align="left">Interviews</TableCell>
            <TableCell align="left">Unapplied</TableCell>
            <TableCell align="left">Listings</TableCell>
            <TableCell align="left">Price</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.length === 0 ? (
            <TableRow>
              <TableCell colSpan={6} align="left">
                <Typography variant="body2" color="text.secondary">
                  No captured points in this range yet.
                </Typography>
              </TableCell>
            </TableRow>
          ) : (
            [...rows].reverse().map((row) => (
              <TableRow key={row.key} hover>
                <TableCell align="left">{row.label}</TableCell>
                <TableCell align="left">{formatCount(row.applied)}</TableCell>
                <TableCell align="left">{formatCount(row.interviews)}</TableCell>
                <TableCell align="left">{formatCount(row.unapplied)}</TableCell>
                <TableCell align="left">{formatCount(row.total)}</TableCell>
                <TableCell align="left">{formatPrice(row.price)}</TableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

export function JobApplicationStatisticsTab() {
  const boardQuery = useQuery({
    queryKey: jobStatisticsQueryKey,
    queryFn: getJobStatisticsBoard,
  });
  const [range, setRange] = useState<RangeMode>("today");
  const [metric, setMetric] = useState<MetricKey>("applied");
  const [profileId, setProfileId] = useState<string | "all">("all");
  const board = boardQuery.data;
  const loading = isQueryLoading(boardQuery.data, boardQuery.isPending);
  const profiles = board?.profiles ?? [];
  const history = board?.history ?? [];

  const periodRows = useMemo(() => {
    if (range === "today") {
      return [];
    }

    return rollupPoints(filterHistory(history, profileId), range);
  }, [history, profileId, range]);

  const selectedProfiles = useMemo(() => {
    if (profileId === "all") {
      return profiles;
    }

    return profiles.filter((item) => item.profileId === profileId);
  }, [profileId, profiles]);

  const summary = useMemo(() => {
    if (range === "today") {
      const rows = selectedProfiles;
      return {
        applied: rows.reduce((sum, item) => sum + item.applied, 0),
        interviews: rows.reduce((sum, item) => sum + item.interviews, 0),
        unapplied: rows.reduce((sum, item) => sum + item.unapplied, 0),
        total: rows.reduce((sum, item) => sum + item.total, 0),
        price: rows.reduce((sum, item) => sum + item.price, 0),
      };
    }

    const latest = periodRows.at(-1);
    return {
      applied: latest?.applied ?? 0,
      interviews: latest?.interviews ?? 0,
      unapplied: latest?.unapplied ?? 0,
      total: latest?.total ?? 0,
      price: latest?.price ?? 0,
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
            Applies, prices, and blank-status listings for each profile. Today uses the live sheet.
            Hour, day, week, and month views use captured hourly stock levels (about 3 months).
          </Typography>
        </Stack>
        <Button
          variant="text"
          onClick={() => void boardQuery.refetch()}
          disabled={boardQuery.isFetching}
          sx={{ alignSelf: { xs: "stretch", md: "flex-start" }, flexShrink: 0 }}
        >
          Refresh
        </Button>
      </Stack>

      <ToggleButtonGroup
        exclusive
        size="small"
        value={range}
        onChange={(_event, value: RangeMode | null) => {
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
              Unapplied means the Status cell is blank on the profile main sheet. Expired, Banned,
              Invalid, and Other are not counted as unapplied.
            </Typography>
            {loading ? (
              <Typography variant="body2" color="text.secondary">
                Loading…
              </Typography>
            ) : (
              <TodayTable profiles={selectedProfiles} />
            )}
          </Stack>
        </Panel>
      ) : (
        <>
          <Panel>
            <Stack spacing={2}>
              <Stack
                direction={{ xs: "column", sm: "row" }}
                spacing={1.5}
                sx={{ alignItems: { sm: "center" }, justifyContent: "space-between" }}
              >
                <Typography variant="subtitle1">Trend</Typography>
                <ToggleButtonGroup
                  exclusive
                  size="small"
                  value={metric}
                  onChange={(_event, value: MetricKey | null) => {
                    if (value) {
                      setMetric(value);
                    }
                  }}
                >
                  <ToggleButton value="applied">Applied</ToggleButton>
                  <ToggleButton value="price">Price</ToggleButton>
                  <ToggleButton value="unapplied">Unapplied</ToggleButton>
                </ToggleButtonGroup>
              </Stack>
              {loading ? (
                <Typography variant="body2" color="text.secondary">
                  Loading…
                </Typography>
              ) : (
                <StatisticsChart rows={periodRows} metric={metric} />
              )}
            </Stack>
          </Panel>
          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="subtitle1">Period detail</Typography>
              <PeriodTable rows={periodRows} />
            </Stack>
          </Panel>
        </>
      )}
    </Stack>
  );
}
