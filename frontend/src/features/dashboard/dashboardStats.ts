import { ApiError } from "@/shared/api/client";
import { appPaths } from "@/shared/config/paths";
import type { JobApplicationLog, JobFinancialBoard, JobFinancialRow } from "@/shared/types/jobApplication";
import type { GoogleConnectionStatus } from "@/shared/types/google";
import type { HealthStatusDto } from "@/shared/types/health";
import type { JobPipelineBoard } from "@/shared/types/pipeline";
import type { UserDto } from "@/shared/types/user";

export type StatusMix = {
  open: number;
  applied: number;
  interviews: number;
  total: number;
  openShare: number;
  appliedShare: number;
  interviewShare: number;
  progressedShare: number;
};

export type PriceBar = {
  entryId: string;
  label: string;
  price: number;
  share: number;
};

export type DayActivity = {
  key: string;
  label: string;
  count: number;
  share: number;
};

export type AttentionItem = {
  id: string;
  severity: "warning" | "info" | "success";
  title: string;
  detail: string;
  to: string;
  action: string;
};

export type UserSummary = {
  total: number;
  active: number;
  inactive: number;
  admins: number;
};

export function queryErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return "Request failed.";
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

export function formatPercent(value: number): string {
  if (!Number.isFinite(value)) {
    return "—";
  }
  return new Intl.NumberFormat(undefined, {
    style: "percent",
    maximumFractionDigits: value > 0 && value < 0.01 ? 2 : 1,
  }).format(value);
}

export function formatRate(value: number): string {
  return String(Number(value.toFixed(4)));
}

export function formatWhen(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));
}

export function locationCount(board: JobPipelineBoard | undefined): number {
  if (!board) {
    return 0;
  }
  return board.sources.reduce((sum, source) => sum + source.locations.length, 0);
}

export function statusMix(board: JobFinancialBoard | undefined): StatusMix {
  const total = board?.allTotal ?? 0;
  const applied = board?.allApplied ?? 0;
  const interviews = board?.allInterviews ?? 0;
  const open = Math.max(0, total - applied - interviews);
  const denom = total > 0 ? total : 1;
  return {
    open,
    applied,
    interviews,
    total,
    openShare: open / denom,
    appliedShare: applied / denom,
    interviewShare: interviews / denom,
    progressedShare: total > 0 ? Math.min(1, (applied + interviews) / total) : 0,
  };
}

export function priceBars(rows: JobFinancialRow[] | undefined, limit = 8): PriceBar[] {
  if (!rows || rows.length === 0) {
    return [];
  }
  const ranked = [...rows].sort((left, right) => right.price - left.price).slice(0, limit);
  const peak = Math.max(...ranked.map((row) => row.price), 0);
  return ranked.map((row) => ({
    entryId: row.entryId,
    label: `${row.profileTitle} · ${row.sourceLabel}`,
    price: row.price,
    share: peak > 0 ? row.price / peak : 0,
  }));
}

export function activityByDay(logs: JobApplicationLog[] | undefined, dayCount = 7): DayActivity[] {
  const items = logs ?? [];
  const counts = new Map<string, number>();
  for (const item of items) {
    const key = toDayKey(new Date(item.occurredAt));
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }

  const now = new Date();
  const days: DayActivity[] = [];
  for (let offset = dayCount - 1; offset >= 0; offset -= 1) {
    const day = new Date(now.getFullYear(), now.getMonth(), now.getDate() - offset);
    const key = toDayKey(day);
    days.push({
      key,
      label: new Intl.DateTimeFormat(undefined, { weekday: "short" }).format(day),
      count: counts.get(key) ?? 0,
      share: 0,
    });
  }

  const peak = Math.max(...days.map((day) => day.count), 0);
  return days.map((day) => ({
    ...day,
    share: peak > 0 ? day.count / peak : 0,
  }));
}

export function summarizeUsers(users: UserDto[] | undefined): UserSummary {
  const list = users ?? [];
  return {
    total: list.length,
    active: list.filter((user) => user.isActive).length,
    inactive: list.filter((user) => !user.isActive).length,
    admins: list.filter((user) => user.role === "Admin").length,
  };
}

export function attentionItems(input: {
  health: HealthStatusDto | undefined;
  google: GoogleConnectionStatus | undefined;
  pipeline: JobPipelineBoard | undefined;
  financial: JobFinancialBoard | undefined;
  isAdmin: boolean;
}): AttentionItem[] {
  const items: AttentionItem[] = [];
  const health = input.health;
  const google = input.google;
  const pipeline = input.pipeline;
  const financial = input.financial;

  if (health && health.status !== "Healthy") {
    items.push({
      id: "health",
      severity: "warning",
      title: "Platform is not healthy",
      detail: health.checks
        .filter((check) => check.status !== "Healthy")
        .map((check) => check.name)
        .join(", ") || "A dependency check failed.",
      to: appPaths.health,
      action: "Open health",
    });
  }

  if (google && !google.configured) {
    items.push({
      id: "google-client",
      severity: "warning",
      title: "Google Cloud client is not saved",
      detail: input.isAdmin
        ? "Save Client ID and Client secret on Settings before anyone can connect Gmail."
        : "An admin must save the Flexis Google Cloud web client on Settings.",
      to: input.isAdmin ? appPaths.settings : appPaths.help,
      action: input.isAdmin ? "Open Settings" : "Open Help",
    });
  } else if (google && !google.connected) {
    items.push({
      id: "gmail",
      severity: "warning",
      title: "Gmail is not connected",
      detail: "Catalog, Operations, and live sheet counts stay blocked until this account connects Gmail.",
      to: appPaths.jobApplication,
      action: "Open Job Application",
    });
  }

  if (google?.connected && pipeline && pipeline.profiles.length === 0) {
    items.push({
      id: "profiles",
      severity: "info",
      title: "No profiles yet",
      detail: "Create a profile on Job Application Settings. Flexis makes a Google Sheet under Flexis / Job Application / Profiles.",
      to: appPaths.jobApplication,
      action: "Create profile",
    });
  }

  if (google?.connected && pipeline && pipeline.sources.length === 0) {
    items.push({
      id: "sources",
      severity: "info",
      title: "No sources yet",
      detail: "Create a source on Job Application Settings, then add location tabs such as US.",
      to: appPaths.jobApplication,
      action: "Create source",
    });
  }

  if (
    google?.connected &&
    pipeline &&
    pipeline.profiles.length > 0 &&
    pipeline.sources.length > 0 &&
    pipeline.entries.length === 0
  ) {
    items.push({
      id: "pipeline",
      severity: "info",
      title: "Pipeline has no rows",
      detail: "On Operations, pair a profile with a source location, then Update to copy listings onto the profile sheet.",
      to: appPaths.jobApplication,
      action: "Open Operations",
    });
  }

  if (
    google?.connected &&
    pipeline &&
    pipeline.entries.length > 0 &&
    financial &&
    financial.allTotal === 0
  ) {
    items.push({
      id: "listings",
      severity: "info",
      title: "No listings on profile sheets",
      detail: "Run Update on Operations to copy Company Name, Position, Link, and JD from each source location.",
      to: appPaths.jobApplication,
      action: "Open Operations",
    });
  }

  if (financial && financial.allTotal > 0 && financial.allApplied === 0 && financial.allInterviews === 0) {
    items.push({
      id: "status",
      severity: "info",
      title: "No Applied or Interview rows",
      detail: "On the named profile main tab, set Status to Applied or Interview. Financial prices those counts.",
      to: appPaths.jobApplication,
      action: "Open Financial",
    });
  }

  if (items.length === 0 && google?.connected) {
    items.push({
      id: "ready",
      severity: "success",
      title: "Workspace is live",
      detail: "Sheet counts and price refresh with header Google sync every 3 minutes while this tab is visible.",
      to: appPaths.jobApplication,
      action: "Open Job Application",
    });
  }

  return items;
}

function toDayKey(value: Date): string {
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, "0")}-${String(value.getDate()).padStart(2, "0")}`;
}
