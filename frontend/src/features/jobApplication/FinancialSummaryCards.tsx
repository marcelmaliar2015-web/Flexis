import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { formatCount, formatFinancialMetrics, formatPrice } from "@/features/jobApplication/financialUi";

const SummaryGrid = styled(Box)(({ theme }) => ({
  display: "grid",
  gap: theme.spacing(2),
  gridTemplateColumns: "repeat(4, minmax(0, 1fr))",
  [theme.breakpoints.down("lg")]: {
    gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
  },
  [theme.breakpoints.down("sm")]: {
    gridTemplateColumns: "1fr",
  },
}));

const SummaryCard = styled(Box, {
  shouldForwardProp: (prop) => prop !== "tone",
})<{ tone: "today" | "main" | "archived" | "lifetime" }>(({ theme, tone }) => ({
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
  todayReady: number;
  todayNotReady: number;
  todayApplied: number;
  todayInterviews: number;
  mainPrice: number;
  mainTotal: number;
  mainReady: number;
  mainNotReady: number;
  mainApplied: number;
  mainInterviews: number;
  archivedPrice: number;
  archivedTotal: number;
  archivedReady: number;
  archivedNotReady: number;
  archivedApplied: number;
  archivedInterviews: number;
  lifetimePrice: number;
  lifetimeTotal: number;
  lifetimeReady: number;
  lifetimeNotReady: number;
  lifetimeApplied: number;
  lifetimeInterviews: number;
};

export function FinancialSummaryCards({
  loading,
  todayPrice,
  todayTotal,
  todayReady,
  todayNotReady,
  todayApplied,
  todayInterviews,
  mainPrice,
  mainTotal,
  mainReady,
  mainNotReady,
  mainApplied,
  mainInterviews,
  archivedPrice,
  archivedTotal,
  archivedReady,
  archivedNotReady,
  archivedApplied,
  archivedInterviews,
  lifetimePrice,
  lifetimeTotal,
  lifetimeReady,
  lifetimeNotReady,
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
            {loading ? "Reading last Update rows." : "Source rows from the last Update, including skips."}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {loading
              ? "…"
              : formatFinancialMetrics(todayApplied, todayInterviews, todayTotal, todayReady, todayNotReady)}
          </Typography>
        </Stack>
      </SummaryCard>
      <SummaryCard tone="main">
        <Stack spacing={1}>
          <Typography variant="overline" color="text.secondary">
            Main
          </Typography>
          <PriceValue variant="h4">{loading ? "…" : formatPrice(mainPrice)}</PriceValue>
          <Typography variant="body2" color="text.secondary">
            {loading ? "Reading current main tabs." : "Full profile main sheet."}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {loading
              ? "…"
              : formatFinancialMetrics(mainApplied, mainInterviews, mainTotal, mainReady, mainNotReady)}
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
              : formatFinancialMetrics(
                  archivedApplied,
                  archivedInterviews,
                  archivedTotal,
                  archivedReady,
                  archivedNotReady,
                )}
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
            {loading ? "Combining main and archived." : "Main plus every archived sheet."}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {loading
              ? "…"
              : formatFinancialMetrics(
                  lifetimeApplied,
                  lifetimeInterviews,
                  lifetimeTotal,
                  lifetimeReady,
                  lifetimeNotReady,
                )}
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
