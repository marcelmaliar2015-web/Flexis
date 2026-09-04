export function asFiniteNumber(value: number | null | undefined, fallback = 0): number {
  return typeof value === "number" && Number.isFinite(value) ? value : fallback;
}

export function formatRate(value: number): string {
  return String(Number(asFiniteNumber(value).toFixed(4)));
}

export function parseRate(value: string): number | null {
  const parsed = Number(value.trim());
  if (!Number.isFinite(parsed) || parsed < 0 || parsed > 10000) {
    return null;
  }

  return Number(parsed.toFixed(4));
}

export function formatPrice(value: number | null | undefined): string {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(asFiniteNumber(value));
}

export function formatCount(value: number | null | undefined): string {
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 0 }).format(asFiniteNumber(value));
}

export function formatFinancialMetrics(
  applied: number | null | undefined,
  interviews: number | null | undefined,
  total: number | null | undefined,
  ready?: number | null,
  notReady?: number | null,
): string {
  if (ready === undefined && notReady === undefined) {
    return `${formatCount(applied)} applied · ${formatCount(interviews)} interviews · ${formatCount(total)} listings`;
  }

  return `${formatCount(ready)} ready · ${formatCount(notReady)} not ready · ${formatCount(applied)} applied · ${formatCount(interviews)} interviews`;
}
