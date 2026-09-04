import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import {
  formatUsageTokens,
  formatUsageUsd,
  MailCheckUsageChart,
} from "@/features/mailCheck/MailCheckUsageChart";
import { getMailCheckUsage, mailCheckUsageQueryKey } from "@/shared/api/mailCheck";
import { isQueryLoading } from "@/shared/api/queryState";

const SummaryGrid = styled(Box)(({ theme }) => ({
  display: "grid",
  gap: theme.spacing(2),
  gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
  [theme.breakpoints.down("sm")]: {
    gridTemplateColumns: "1fr",
  },
}));

const SummaryCard = styled(Box, {
  shouldForwardProp: (prop) => prop !== "tone",
})<{ tone: "today" | "lifetime" }>(({ theme, tone }) => ({
  minWidth: 0,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2.5),
  border:
    tone === "today"
      ? `1px solid ${theme.palette.primary.main}`
      : `1px solid ${theme.palette.secondary.main}`,
  backgroundColor: theme.palette.background.paper,
  boxShadow:
    tone === "lifetime"
      ? "0 12px 32px rgba(14, 39, 68, 0.08)"
      : "0 8px 24px rgba(30, 77, 107, 0.08)",
}));

const ValueText = styled(Typography)(({ theme }) => ({
  fontVariantNumeric: "tabular-nums",
  letterSpacing: "-0.02em",
  color: theme.palette.text.primary,
}));

export function MailCheckUsageTab() {
  const usageQuery = useQuery({
    queryKey: mailCheckUsageQueryKey,
    queryFn: getMailCheckUsage,
    refetchInterval: 60_000,
  });
  const loading = isQueryLoading(usageQuery.data, usageQuery.isPending);
  const lifetime = usageQuery.data?.lifetime;
  const today = usageQuery.data?.today;
  const history = usageQuery.data?.history ?? [];

  return (
    <Stack spacing={2.5}>
      <SummaryGrid>
        <SummaryCard tone="today">
          <Stack spacing={1}>
            <Typography variant="overline" color="primary">
              Today
            </Typography>
            <ValueText variant="h4">
              {loading ? "…" : formatUsageUsd(today?.estimatedCostUsd ?? 0)}
            </ValueText>
            <Typography variant="body2" color="text.secondary">
              {loading
                ? "Loading…"
                : `${formatUsageTokens(today?.totalTokens ?? 0)} tokens · ${formatUsageTokens(today?.callCount ?? 0)} calls`}
            </Typography>
          </Stack>
        </SummaryCard>
        <SummaryCard tone="lifetime">
          <Stack spacing={1}>
            <Typography variant="overline" color="secondary">
              Lifetime
            </Typography>
            <ValueText variant="h4">
              {loading ? "…" : formatUsageUsd(lifetime?.estimatedCostUsd ?? 0)}
            </ValueText>
            <Typography variant="body2" color="text.secondary">
              {loading
                ? "Loading…"
                : `${formatUsageTokens(lifetime?.totalTokens ?? 0)} tokens · ${formatUsageTokens(lifetime?.callCount ?? 0)} calls`}
            </Typography>
          </Stack>
        </SummaryCard>
      </SummaryGrid>
      <MailCheckUsageChart history={history} loading={loading} />
      <Typography variant="caption" color="text.secondary">
        Estimates use OpenAI list rates for common models (including gpt-4.1-mini and gpt-4o-mini).
        Actual invoice amounts can differ with credits, cached tokens, or private pricing.
      </Typography>
    </Stack>
  );
}
