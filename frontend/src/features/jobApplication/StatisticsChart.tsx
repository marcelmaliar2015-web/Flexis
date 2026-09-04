import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled, useTheme } from "@mui/material/styles";
import { useState } from "react";
import { formatCount, formatPrice } from "@/features/jobApplication/financialUi";
import type { StatisticsPeriodRow } from "@/features/jobApplication/statisticsPeriod";

const ChartFrame = styled(Box)({
  position: "relative",
  width: "100%",
  height: 320,
});

const ChartSvg = styled("svg")({
  display: "block",
  width: "100%",
  height: "100%",
});

type StatisticsChartProps = {
  rows: StatisticsPeriodRow[];
};

type SeriesKey = "applied" | "interviews" | "price";

export function StatisticsChart({ rows }: StatisticsChartProps) {
  const theme = useTheme();
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);
  const width = 760;
  const height = 320;
  const pad = { top: 24, right: 56, bottom: 48, left: 52 };
  const plotWidth = width - pad.left - pad.right;
  const plotHeight = height - pad.top - pad.bottom;
  const countMax = Math.max(...rows.flatMap((row) => [row.applied, row.interviews]), 1);
  const priceMax = Math.max(...rows.map((row) => row.price), 1);
  const series: { key: SeriesKey; label: string; color: string; value: (row: StatisticsPeriodRow) => number; max: number }[] = [
    {
      key: "applied",
      label: "Applied",
      color: theme.palette.primary.main,
      value: (row) => row.applied,
      max: countMax,
    },
    {
      key: "interviews",
      label: "Interview",
      color: theme.palette.info.main,
      value: (row) => row.interviews,
      max: countMax,
    },
    {
      key: "price",
      label: "Price",
      color: theme.palette.secondary.main,
      value: (row) => row.price,
      max: priceMax,
    },
  ];

  if (rows.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        No history yet. Run Update, then open Statistics or Financial while Gmail can read sheets so
        status changes are recorded with timestamps.
      </Typography>
    );
  }

  function xAt(index: number): number {
    return pad.left + (rows.length === 1 ? plotWidth / 2 : (plotWidth * index) / (rows.length - 1));
  }

  function yAt(value: number, max: number): number {
    return pad.top + plotHeight - (value / max) * plotHeight;
  }

  const hover = hoverIndex === null ? null : rows[hoverIndex];

  return (
    <Stack spacing={1.5}>
      <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap", gap: 1 }}>
        {series.map((item) => (
          <Stack key={item.key} direction="row" spacing={0.75} sx={{ alignItems: "center" }}>
            <Box sx={{ width: 12, height: 3, bgcolor: item.color, borderRadius: 1 }} />
            <Typography variant="caption" color="text.secondary">
              {item.label}
            </Typography>
          </Stack>
        ))}
      </Stack>
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
                  {formatCount(Math.round(countMax * share))}
                </text>
                <text
                  x={width - pad.right + 8}
                  y={y + 4}
                  textAnchor="start"
                  fill={theme.palette.text.secondary}
                  fontSize={11}
                >
                  {formatPrice(priceMax * share)}
                </text>
              </g>
            );
          })}
          {series.map((item) => {
            const points = rows.map((row, index) => ({
              x: xAt(index),
              y: yAt(item.value(row), item.max),
            }));
            const path = points
              .map((point, index) => `${index === 0 ? "M" : "L"} ${point.x.toFixed(1)} ${point.y.toFixed(1)}`)
              .join(" ");
            return (
              <g key={item.key}>
                <path d={path} fill="none" stroke={item.color} strokeWidth={2.5} strokeLinejoin="round" />
                {points.map((point, index) => (
                  <circle
                    key={`${item.key}-${rows[index].key}`}
                    cx={point.x}
                    cy={point.y}
                    r={hoverIndex === index ? 5 : 3.5}
                    fill={item.color}
                  />
                ))}
              </g>
            );
          })}
          {rows.map((row, index) => (
            <rect
              key={`hit-${row.key}`}
              x={xAt(index) - plotWidth / Math.max(rows.length * 2, 2)}
              y={pad.top}
              width={Math.max(plotWidth / Math.max(rows.length, 1), 24)}
              height={plotHeight}
              fill="transparent"
              onMouseEnter={() => setHoverIndex(index)}
              onMouseLeave={() => setHoverIndex(null)}
            />
          ))}
          {rows.length <= 12
            ? rows.map((row, index) => (
                <text
                  key={`label-${row.key}`}
                  x={xAt(index)}
                  y={height - 14}
                  textAnchor="middle"
                  fill={theme.palette.text.secondary}
                  fontSize={10}
                >
                  {row.label}
                </text>
              ))
            : null}
        </ChartSvg>
        {hover && hoverIndex !== null ? (
          <Box
            sx={{
              position: "absolute",
              left: Math.min(xAt(hoverIndex) + 8, width - 200),
              top: 8,
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
              {hover.label}
            </Typography>
            <Typography variant="body2">{formatCount(hover.applied)} applied</Typography>
            <Typography variant="body2">{formatCount(hover.interviews)} interviews</Typography>
            <Typography variant="body2">{formatPrice(hover.price)} price</Typography>
          </Box>
        ) : null}
      </ChartFrame>
    </Stack>
  );
}
