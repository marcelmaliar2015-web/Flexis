export function formatRate(value: number): string {
  return String(Number(value.toFixed(4)));
}

export function parseRate(value: string): number | null {
  const parsed = Number(value.trim());
  if (!Number.isFinite(parsed) || parsed < 0 || parsed > 10000) {
    return null;
  }

  return Number(parsed.toFixed(4));
}

export function formatPrice(value: number): string {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

export function formatCount(value: number): string {
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 0 }).format(value);
}

export function formatFinancialMetrics(applied: number, interviews: number, total: number): string {
  return `${formatCount(applied)} applied · ${formatCount(interviews)} interviews · ${formatCount(total)} listings`;
}
