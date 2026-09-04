import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import { styled, useTheme } from "@mui/material/styles";
import { useMemo, useState } from "react";
import type { MailCheckUsageHour } from "@/shared/types/mailCheck";

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
  minWidth: 200,
  pointerEvents: "none",
  border: `1px solid ${theme.palette.divider}`,
  borderRadius: theme.shape.borderRadius,
  backgroundColor: theme.palette.background.paper,
  boxShadow: "0 10px 28px rgba(14, 39, 68, 0.12)",
  padding: theme.spacing(1.25, 1.5),
}));

type RangeMode = "hourly" | "daily";

type SeriesKey = "estimatedCostUsd" | "totalTokens" | "callCount";

type ChartPoint = {
  key: string;
  label: string;
  estimatedCostUsd: number;
  totalTokens: number;
  callCount: number;
  promptTokens: number;
  completionTokens: number;
  lastModel: string;
};

type Series = {
  key: SeriesKey;
  label: string;
  color: string;
  scaleMax: number;
};

function dayKey(iso: string): string {
  return iso.slice(0, 10);
}

function formatUsd(value: number): string {
  if (value > 0 && value < 0.01) {
    return `$${value.toFixed(4)}`;
  }

  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 4,
  }).format(value);
}

function formatTokens(value: number): string {
  return new Intl.NumberFormat(undefined).format(Math.round(value));
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

function toChartPoints(history: MailCheckUsageHour[], mode: RangeMode): ChartPoint[] {
  if (mode === "hourly") {
    return history.map((item) => ({
      key: item.capturedHour,
      label: formatHourLabel(item.capturedHour),
      estimatedCostUsd: item.estimatedCostUsd,
      totalTokens: item.totalTokens,
      callCount: item.callCount,
      promptTokens: item.promptTokens,
      completionTokens: item.completionTokens,
      lastModel: item.lastModel,
    }));
  }

  const byDay = new Map<string, ChartPoint>();
  for (const item of history) {
    const key = dayKey(item.capturedOn || item.capturedHour);
    const existing = byDay.get(key);
    if (!existing) {
      byDay.set(key, {
        key,
        label: formatDayLabel(key),
        estimatedCostUsd: item.estimatedCostUsd,
        totalTokens: item.totalTokens,
        callCount: item.callCount,
        promptTokens: item.promptTokens,
        completionTokens: item.completionTokens,
        lastModel: item.lastModel,
      });
      continue;
    }

    existing.estimatedCostUsd += item.estimatedCostUsd;
    existing.totalTokens += item.totalTokens;
    existing.callCount += item.callCount;
    existing.promptTokens += item.promptTokens;
    existing.completionTokens += item.completionTokens;
    if (item.lastModel) {
      existing.lastModel = item.lastModel;
    }
  }

  return [...byDay.values()].sort((left, right) => left.key.localeCompare(right.key));
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

function areaPath(coords: Array<{ x: number; y: number }>, baselineY: number): string {
  if (coords.length === 0) {
    return "";
  }

  const start = coords[0];
  const end = coords[coords.length - 1];
  return `${linePath(coords)} L ${end.x.toFixed(1)} ${baselineY.toFixed(1)} L ${start.x.toFixed(1)} ${baselineY.toFixed(1)} Z`;
}

type MailCheckUsageChartProps = {
  history: MailCheckUsageHour[];
  loading: boolean;
};

export function MailCheckUsageChart({ history, loading }: MailCheckUsageChartProps) {
  const theme = useTheme();
  const [mode, setMode] = useState<RangeMode>("hourly");
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);
  const width = 760;
  const height = 280;
  const padLeft = 56;
  const padRight = 52;
  const padTop = 20;
  const padBottom = 36;

  const points = useMemo(() => toChartPoints(history, mode), [history, mode]);
  const maxCost = Math.max(0.01, ...points.map((point) => point.estimatedCostUsd));
  const maxTokens = Math.max(1, ...points.map((point) => point.totalTokens));
  const maxCalls = Math.max(1, ...points.map((point) => point.callCount));
  const series: Series[] = [
    {
      key: "estimatedCostUsd",
      label: "Estimated cost",
      color: theme.palette.primary.main,
      scaleMax: maxCost,
    },
    {
      key: "totalTokens",
      label: "Tokens",
      color: theme.palette.secondary.main,
      scaleMax: maxTokens,
    },
    {
      key: "callCount",
      label: "Calls",
      color: "#5C6672",
      scaleMax: maxCalls,
    },
  ];
  const yTicks = [0, 0.25, 0.5, 0.75, 1].map((ratio) => ({
    ratio,
    cost: maxCost * ratio,
    y: padTop + (height - padTop - padBottom) * (1 - ratio),
  }));
  const baselineY = height - padBottom;
  const seriesCoords = series.map((item) => ({
    ...item,
    coords: pointCoords(
      points,
      item.key,
      item.scaleMax,
      width,
      height,
      padLeft,
      padRight,
      padTop,
      padBottom,
    ),
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
              API key usage
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Hourly OpenAI classify spend from your Mail Check key. Daily sums each hour. Cost is
              estimated from published list rates for the model in use.
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
            aria-label="API usage chart range"
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
            No usage yet. Run Check or leave auto-check on so classify calls record here.
          </Typography>
        ) : (
          <ChartFrame onMouseLeave={() => setHoverIndex(null)}>
            <ChartSvg viewBox={`0 0 ${width} ${height}`} role="img" aria-label="OpenAI usage over time">
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
                    {formatUsd(tick.cost)}
                  </text>
                </g>
              ))}
              {seriesCoords.map((item) => (
                <g key={item.key}>
                  {item.key === "estimatedCostUsd" ? (
                    <path d={areaPath(item.coords, baselineY)} fill={item.color} opacity={0.08} />
                  ) : null}
                  <path
                    d={linePath(item.coords)}
                    fill="none"
                    stroke={item.color}
                    strokeWidth={item.key === "estimatedCostUsd" ? 2.75 : 2}
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
                  left: Math.min(Math.max(hoverX - 100, 8), width - 220),
                  top: 12,
                }}
              >
                <Stack spacing={0.5}>
                  <Typography variant="subtitle2">{hover.label}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Cost {formatUsd(hover.estimatedCostUsd)}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {formatTokens(hover.totalTokens)} tokens · {formatTokens(hover.callCount)} calls
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Prompt {formatTokens(hover.promptTokens)} · Completion{" "}
                    {formatTokens(hover.completionTokens)}
                  </Typography>
                  {hover.lastModel ? (
                    <Typography variant="caption" color="text.secondary">
                      Model {hover.lastModel}
                    </Typography>
                  ) : null}
                </Stack>
              </TooltipCard>
            ) : null}
          </ChartFrame>
        )}
      </Stack>
    </ChartPanel>
  );
}

export function formatUsageUsd(value: number): string {
  return formatUsd(value);
}

export function formatUsageTokens(value: number): string {
  return formatTokens(value);
}
