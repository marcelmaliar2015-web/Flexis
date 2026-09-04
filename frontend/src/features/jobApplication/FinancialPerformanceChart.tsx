import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import { styled, useTheme } from "@mui/material/styles";
import { useMemo, useState } from "react";
import { formatCount, formatPrice, asFiniteNumber } from "@/features/jobApplication/financialUi";
import type { JobFinancialSnapshot } from "@/shared/types/jobApplication";

const ChartPanel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2),
}));

const ChartFrame = styled(Box)({
  position: "relative",
  width: "100%",
  height: 280,
});

const ChartSvg = styled("svg")({
  display: "block",
  width: "100%",
  height: "100%",
});

const TooltipCard = styled(Box)(({ theme }) => ({
  position: "absolute",
  zIndex: 2,
  minWidth: 180,
  pointerEvents: "none",
  border: `1px solid ${theme.palette.divider}`,
  borderRadius: theme.shape.borderRadius,
  backgroundColor: theme.palette.background.paper,
  boxShadow: "0 10px 28px rgba(14, 39, 68, 0.12)",
  padding: theme.spacing(1.25, 1.5),
}));

type RangeMode = "hourly" | "daily";

type SeriesKey = "lifetimePrice" | "todayPrice" | "mainPrice" | "archivedPrice";

type ChartPoint = {
  key: string;
  label: string;
  at: number;
  todayPrice: number;
  mainPrice: number;
  archivedPrice: number;
  lifetimePrice: number;
  todayApplied: number;
  todayInterviews: number;
  mainApplied: number;
  mainInterviews: number;
  lifetimeApplied: number;
  lifetimeInterviews: number;
};

type Series = {
  key: SeriesKey;
  label: string;
  color: string;
};

function dayKey(iso: string): string {
  return iso.slice(0, 10);
}

function toChartPoints(history: JobFinancialSnapshot[], mode: RangeMode): ChartPoint[] {
  if (mode === "hourly") {
    return history.map((item) => {
      const at = Date.parse(item.capturedHour || item.capturedAt);
      return {
        key: item.capturedHour || item.capturedAt,
        label: formatHourLabel(item.capturedHour || item.capturedAt),
        at: Number.isNaN(at) ? 0 : at,
        todayPrice: asFiniteNumber(item.todayPrice),
        mainPrice: asFiniteNumber(item.mainPrice),
        archivedPrice: asFiniteNumber(item.archivedPrice),
        lifetimePrice: asFiniteNumber(item.lifetimePrice),
        todayApplied: asFiniteNumber(item.todayApplied),
        todayInterviews: asFiniteNumber(item.todayInterviews),
        mainApplied: asFiniteNumber(item.mainApplied),
        mainInterviews: asFiniteNumber(item.mainInterviews),
        lifetimeApplied: asFiniteNumber(item.lifetimeApplied),
        lifetimeInterviews: asFiniteNumber(item.lifetimeInterviews),
      };
    });
  }

  const byDay = new Map<string, JobFinancialSnapshot>();
  for (const item of history) {
    byDay.set(dayKey(item.capturedOn || item.capturedHour || item.capturedAt), item);
  }

  return [...byDay.entries()]
    .sort(([left], [right]) => left.localeCompare(right))
    .map(([day, item]) => {
      const at = Date.parse(`${day}T00:00:00Z`);
      return {
        key: day,
        label: formatDayLabel(day),
        at: Number.isNaN(at) ? 0 : at,
        todayPrice: asFiniteNumber(item.todayPrice),
        mainPrice: asFiniteNumber(item.mainPrice),
        archivedPrice: asFiniteNumber(item.archivedPrice),
        lifetimePrice: asFiniteNumber(item.lifetimePrice),
        todayApplied: asFiniteNumber(item.todayApplied),
        todayInterviews: asFiniteNumber(item.todayInterviews),
        mainApplied: asFiniteNumber(item.mainApplied),
        mainInterviews: asFiniteNumber(item.mainInterviews),
        lifetimeApplied: asFiniteNumber(item.lifetimeApplied),
        lifetimeInterviews: asFiniteNumber(item.lifetimeInterviews),
      };
    });
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
  }).format(parsed);
}

function formatDayLabel(value: string): string {
  const parsed = Date.parse(`${value}T00:00:00Z`);
  if (Number.isNaN(parsed)) {
    return value;
  }

  return new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric" }).format(parsed);
}

function valueFor(point: ChartPoint, key: SeriesKey): number {
  return point[key];
}

function pointCoords(
  points: ChartPoint[],
  key: SeriesKey,
  maxValue: number,
  width: number,
  height: number,
  padLeft: number,
  padRight: number,
  padTop: number,
  padBottom: number,
): Array<{ x: number; y: number; point: ChartPoint }> {
  const innerWidth = width - padLeft - padRight;
  const innerHeight = height - padTop - padBottom;
  return points.map((point, index) => {
    const x =
      points.length === 1
        ? padLeft + innerWidth / 2
        : padLeft + (index / (points.length - 1)) * innerWidth;
    const y = padTop + innerHeight - (valueFor(point, key) / maxValue) * innerHeight;
    return { x, y, point };
  });
}

function linePath(coords: Array<{ x: number; y: number }>): string {
  if (coords.length === 0) {
    return "";
  }

  return coords
    .map((coord, index) => `${index === 0 ? "M" : "L"} ${coord.x.toFixed(1)} ${coord.y.toFixed(1)}`)
    .join(" ");
}

function areaPath(
  coords: Array<{ x: number; y: number }>,
  baselineY: number,
): string {
  if (coords.length === 0) {
    return "";
  }

  const start = coords[0];
  const end = coords[coords.length - 1];
  return `${linePath(coords)} L ${end.x.toFixed(1)} ${baselineY.toFixed(1)} L ${start.x.toFixed(1)} ${baselineY.toFixed(1)} Z`;
}

type FinancialPerformanceChartProps = {
  history: JobFinancialSnapshot[];
  loading: boolean;
};

export function FinancialPerformanceChart({ history, loading }: FinancialPerformanceChartProps) {
  const theme = useTheme();
  const [mode, setMode] = useState<RangeMode>("hourly");
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);
  const width = 760;
  const height = 280;
  const padLeft = 52;
  const padRight = 20;
  const padTop = 20;
  const padBottom = 36;
  const series: Series[] = [
    { key: "lifetimePrice", label: "Lifetime", color: theme.palette.primary.main },
    { key: "todayPrice", label: "Today", color: theme.palette.secondary.main },
    { key: "mainPrice", label: "Main", color: theme.palette.info.main },
    { key: "archivedPrice", label: "Archived", color: "#5C6672" },
  ];

  const points = useMemo(() => toChartPoints(history, mode), [history, mode]);
  const maxValue = Math.max(
    1,
    ...points.flatMap((point) => [
      point.lifetimePrice,
      point.todayPrice,
      point.mainPrice,
      point.archivedPrice,
    ]),
  );
  const yTicks = [0, 0.25, 0.5, 0.75, 1].map((ratio) => ({
    ratio,
    value: maxValue * ratio,
    y: padTop + (height - padTop - padBottom) * (1 - ratio),
  }));
  const baselineY = height - padBottom;
  const seriesCoords = series.map((item) => ({
    ...item,
    coords: pointCoords(points, item.key, maxValue, width, height, padLeft, padRight, padTop, padBottom),
  }));
  const xLabels =
    points.length <= 1
      ? points.map((point, index) => ({ index, label: point.label }))
      : [0, Math.floor((points.length - 1) / 2), points.length - 1]
          .filter((index, position, all) => all.indexOf(index) === position)
          .map((index) => ({ index, label: points[index].label }));
  const hover = hoverIndex === null ? null : points[hoverIndex];
  const hoverX =
    hoverIndex === null || points.length === 0
      ? null
      : points.length === 1
        ? padLeft + (width - padLeft - padRight) / 2
        : padLeft + (hoverIndex / (points.length - 1)) * (width - padLeft - padRight);

  return (
    <ChartPanel>
      <Stack spacing={1.5}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={1.5}
          sx={{ alignItems: { sm: "flex-start" }, justifyContent: "space-between" }}
        >
          <Stack spacing={0.25}>
            <Typography variant="h6" component="h2">
              Performance
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Hourly snapshots of workspace price. Switch to daily for the last value each day. Hover a
              point for Applied, Interviews, and price detail.
            </Typography>
          </Stack>
          <ToggleButtonGroup
            exclusive
            size="small"
            value={mode}
            onChange={(_event, next: RangeMode | null) => {
              if (next) {
                setMode(next);
                setHoverIndex(null);
              }
            }}
            aria-label="Performance chart range"
          >
            <ToggleButton value="hourly">Hourly</ToggleButton>
            <ToggleButton value="daily">Daily</ToggleButton>
          </ToggleButtonGroup>
        </Stack>
        <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap" }}>
          {series.map((item) => (
            <Stack key={item.key} direction="row" spacing={0.75} sx={{ alignItems: "center" }}>
              <Box sx={{ width: 14, height: 3, backgroundColor: item.color, borderRadius: 999 }} />
              <Typography variant="caption" color="text.secondary">
                {item.label}
              </Typography>
            </Stack>
          ))}
        </Stack>
        {loading ? (
          <Typography variant="body2" color="text.secondary">
            Loading…
          </Typography>
        ) : points.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No history yet. Open this tab or refresh to save the first hourly snapshot.
          </Typography>
        ) : (
          <ChartFrame
            onMouseLeave={() => setHoverIndex(null)}
          >
            <ChartSvg viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Financial price over time">
              {yTicks.map((tick) => (
                <g key={tick.ratio}>
                  <line
                    x1={padLeft}
                    x2={width - padRight}
                    y1={tick.y}
                    y2={tick.y}
                    stroke={theme.palette.divider}
                    strokeWidth={1}
                  />
                  <text
                    x={padLeft - 8}
                    y={tick.y + 3}
                    textAnchor="end"
                    fill={theme.palette.text.secondary}
                    fontSize="11"
                  >
                    {formatPrice(tick.value)}
                  </text>
                </g>
              ))}
              {seriesCoords.map((item) => (
                <g key={item.key}>
                  {item.key === "lifetimePrice" ? (
                    <path
                      d={areaPath(item.coords, baselineY)}
                      fill={item.color}
                      opacity={0.08}
                    />
                  ) : null}
                  <path
                    d={linePath(item.coords)}
                    fill="none"
                    stroke={item.color}
                    strokeWidth={item.key === "lifetimePrice" ? 2.75 : 2}
                    strokeLinejoin="round"
                    strokeLinecap="round"
                  />
                  {item.coords.map((coord, index) => (
                    <circle
                      key={`${item.key}-${coord.point.key}`}
                      cx={coord.x}
                      cy={coord.y}
                      r={hoverIndex === index ? 5 : points.length === 1 ? 5 : 3.5}
                      fill={theme.palette.background.paper}
                      stroke={item.color}
                      strokeWidth={2}
                    />
                  ))}
                </g>
              ))}
              {xLabels.map((item) => {
                const x =
                  points.length === 1
                    ? padLeft + (width - padLeft - padRight) / 2
                    : padLeft + (item.index / (points.length - 1)) * (width - padLeft - padRight);
                return (
                  <text
                    key={`${item.index}-${item.label}`}
                    x={x}
                    y={height - 10}
                    textAnchor="middle"
                    fill={theme.palette.text.secondary}
                    fontSize="11"
                  >
                    {item.label}
                  </text>
                );
              })}
              {points.map((point, index) => {
                const x =
                  points.length === 1
                    ? padLeft + (width - padLeft - padRight) / 2
                    : padLeft + (index / (points.length - 1)) * (width - padLeft - padRight);
                return (
                  <rect
                    key={`hit-${point.key}`}
                    x={x - 12}
                    y={padTop}
                    width={24}
                    height={height - padTop - padBottom}
                    fill="transparent"
                    onMouseEnter={() => setHoverIndex(index)}
                  />
                );
              })}
              {hoverX !== null ? (
                <line
                  x1={hoverX}
                  x2={hoverX}
                  y1={padTop}
                  y2={baselineY}
                  stroke={theme.palette.text.secondary}
                  strokeDasharray="4 4"
                  strokeWidth={1}
                />
              ) : null}
            </ChartSvg>
            {hover && hoverX !== null ? (
              <TooltipCard
                style={{
                  left: Math.min(Math.max(hoverX - 90, 8), width - 200),
                  top: 12,
                }}
              >
                <Stack spacing={0.5}>
                  <Typography variant="subtitle2">{hover.label}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Lifetime {formatPrice(hover.lifetimePrice)} ·{" "}
                    {formatCount(hover.lifetimeApplied)} applied ·{" "}
                    {formatCount(hover.lifetimeInterviews)} interviews
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Today {formatPrice(hover.todayPrice)} · {formatCount(hover.todayApplied)} applied ·{" "}
                    {formatCount(hover.todayInterviews)} interviews
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Main {formatPrice(hover.mainPrice)} · {formatCount(hover.mainApplied)} applied ·{" "}
                    {formatCount(hover.mainInterviews)} interviews
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Archived {formatPrice(hover.archivedPrice)}
                  </Typography>
                </Stack>
              </TooltipCard>
            ) : null}
          </ChartFrame>
        )}
      </Stack>
    </ChartPanel>
  );
}
