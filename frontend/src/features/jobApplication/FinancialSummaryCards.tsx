import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { formatCount, formatFinancialMetrics, formatPrice } from "@/features/jobApplication/financialUi";

const SummaryGrid = styled(Box)(({ theme }) => ({
  display: "grid",
  gap: theme.spacing(2),
  gridTemplateColumns: "repeat(3, minmax(0, 1fr))",
  [theme.breakpoints.down("md")]: {
    gridTemplateColumns: "1fr",
  },
}));

const SummaryCard = styled(Box, {
  shouldForwardProp: (prop) => prop !== "tone",
})<{ tone: "today" | "archived" | "lifetime" }>(({ theme, tone }) => ({
  minWidth: 0,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2.5),
  border:
    tone === "today"
      ? `1px solid ${theme.palette.primary.main}`
      : tone === "lifetime"
        ? `1px solid ${theme.palette.secondary.main}`
        : `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  boxShadow:
    tone === "lifetime"
      ? "0 12px 32px rgba(14, 39, 68, 0.08)"
      : tone === "today"
        ? "0 8px 24px rgba(30, 77, 107, 0.08)"
        : "none",
}));

const PriceValue = styled(Typography)(({ theme }) => ({
  fontVariantNumeric: "tabular-nums",
  letterSpacing: "-0.02em",
  color: theme.palette.text.primary,
}));

type FinancialSummaryCardsProps = {
  loading: boolean;
  todayPrice: number;
  todayTotal: number;
  todayApplied: number;
  todayInterviews: number;
  archivedPrice: number;
  archivedTotal: number;
  archivedApplied: number;
  archivedInterviews: number;
  lifetimePrice: number;
  lifetimeTotal: number;
  lifetimeApplied: number;
  lifetimeInterviews: number;
};

export function FinancialSummaryCards({
  loading,
  todayPrice,
  todayTotal,
  todayApplied,
  todayInterviews,
  archivedPrice,
  archivedTotal,
  archivedApplied,
  archivedInterviews,
  lifetimePrice,
  lifetimeTotal,
  lifetimeApplied,
  lifetimeInterviews,
}: FinancialSummaryCardsProps) {
  return (
    <SummaryGrid>
      <SummaryCard tone="today">
        <Stack spacing={1}>
          <Typography variant="overline" color="primary">
            Today
          </Typography>
          <PriceValue variant="h4">{loading ? "…" : formatPrice(todayPrice)}</PriceValue>
          <Typography variant="body2" color="text.secondary">
            {loading
              ? "Reading current main tabs."
              : "Current profile main tab only."}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {loading
              ? "…"
              : formatFinancialMetrics(todayApplied, todayInterviews, todayTotal)}
          </Typography>
        </Stack>
      </SummaryCard>
      <SummaryCard tone="archived">
        <Stack spacing={1}>
          <Typography variant="overline" color="text.secondary">
            Archived
          </Typography>
          <PriceValue variant="h4">{loading ? "…" : formatPrice(archivedPrice)}</PriceValue>
          <Typography variant="body2" color="text.secondary">
            {loading ? "Reading numbered sheets." : "Forwarded sheets named 1, 2, 3…"}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {loading
              ? "…"
              : formatFinancialMetrics(archivedApplied, archivedInterviews, archivedTotal)}
          </Typography>
        </Stack>
      </SummaryCard>
      <SummaryCard tone="lifetime">
        <Stack spacing={1}>
          <Typography variant="overline" color="secondary">
            Lifetime
          </Typography>
          <PriceValue variant="h4">{loading ? "…" : formatPrice(lifetimePrice)}</PriceValue>
          <Typography variant="body2" color="text.secondary">
            {loading ? "Combining today and archived." : "Today plus every archived sheet."}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {loading
              ? "…"
              : formatFinancialMetrics(lifetimeApplied, lifetimeInterviews, lifetimeTotal)}
          </Typography>
        </Stack>
      </SummaryCard>
    </SummaryGrid>
  );
}

type FinancialMetricCellProps = {
  applied: number;
  interviews: number;
  price: number;
  emphasize?: boolean;
};

export function FinancialMetricCell({ applied, interviews, price, emphasize = false }: FinancialMetricCellProps) {
  return (
    <Stack spacing={0.25} sx={{ minWidth: 108 }}>
      <Typography variant="caption" color="text.secondary">
        {formatCount(applied)} app · {formatCount(interviews)} int
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: emphasize ? 700 : 600 }}>
        {formatPrice(price)}
      </Typography>
    </Stack>
  );
}
