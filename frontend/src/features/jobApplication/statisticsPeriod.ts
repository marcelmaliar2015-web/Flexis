import type { JobStatisticsPoint } from "@/shared/types/jobApplication";

export type StatisticsRangeMode = "today" | "hourly" | "daily" | "weekly" | "monthly";

export type StatisticsPeriodRow = {
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

export function filterStatisticsHistory(
  history: JobStatisticsPoint[],
  profileId: string | "all",
): JobStatisticsPoint[] {
  if (profileId === "all") {
    return history;
  }

  return history.filter((item) => item.profileId === profileId);
}

export function rollupStatisticsPoints(
  points: JobStatisticsPoint[],
  mode: Exclude<StatisticsRangeMode, "today">,
): StatisticsPeriodRow[] {
  const byPeriod = new Map<string, StatisticsPeriodRow>();
  const latestUnappliedByPeriodProfile = new Map<string, { hour: string; unapplied: number }>();

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

    const unappliedKey = `${period}::${point.profileId}`;
    const existingUnapplied = latestUnappliedByPeriodProfile.get(unappliedKey);
    if (!existingUnapplied || Date.parse(point.capturedHour) >= Date.parse(existingUnapplied.hour)) {
      latestUnappliedByPeriodProfile.set(unappliedKey, {
        hour: point.capturedHour,
        unapplied: point.unapplied,
      });
    }

    const current = byPeriod.get(period);
    if (current) {
      current.applied += point.applied;
      current.interviews += point.interviews;
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
      unapplied: 0,
      total: point.total,
      price: point.price,
    });
  }

  for (const [mapKey, value] of latestUnappliedByPeriodProfile) {
    const period = mapKey.slice(0, mapKey.indexOf("::"));
    const row = byPeriod.get(period);
    if (row) {
      row.unapplied += value.unapplied;
    }
  }

  return [...byPeriod.values()].sort((left, right) => left.key.localeCompare(right.key));
}
