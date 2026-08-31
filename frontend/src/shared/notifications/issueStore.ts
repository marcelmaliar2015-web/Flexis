import { getAccessToken } from "@/shared/api/accessToken";
import { getApiBaseUrl } from "@/shared/config/env";
import type { IssueDraft, IssueNotice } from "@/shared/types/issue";

const storageKey = "flexis.issueNotices";
const dismissedToastKey = "flexis.dismissedIssueToasts";
const maxItems = 50;
const maxDismissedToasts = 100;
const dedupeMs = 180_000;
const restoreToastMs = 120_000;
const diagnosticsPath = "/api/diagnostics/events";

type Listener = () => void;

let items: IssueNotice[] = readStored();
let toast: IssueNotice | null = null;
const listeners = new Set<Listener>();

function emit() {
  listeners.forEach((listener) => listener());
}

function readStored(): IssueNotice[] {
  try {
    const raw = window.localStorage.getItem(storageKey);
    if (!raw) {
      return [];
    }
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) {
      return [];
    }
    return parsed.filter(isNotice).slice(0, maxItems);
  } catch {
    return [];
  }
}

function isNotice(value: unknown): value is IssueNotice {
  if (!value || typeof value !== "object") {
    return false;
  }
  const record = value as Record<string, unknown>;
  return (
    typeof record.id === "string" &&
    typeof record.occurredAt === "string" &&
    (record.severity === "error" || record.severity === "warning") &&
    typeof record.source === "string" &&
    typeof record.message === "string"
  );
}

function persist() {
  try {
    window.localStorage.setItem(storageKey, JSON.stringify(items));
  } catch {
    return;
  }
}

function makeId(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function pushToServer(notice: IssueNotice) {
  if (notice.source === "api" && notice.status !== undefined && notice.status !== 0 && notice.status !== 502 && notice.status !== 504) {
    return;
  }
  const token = getAccessToken();
  if (!token) {
    return;
  }

  const headers = new Headers();
  headers.set("Accept", "application/json");
  headers.set("Content-Type", "application/json");
  headers.set("Authorization", `Bearer ${token}`);
  void fetch(`${getApiBaseUrl()}${diagnosticsPath}`, {
    method: "POST",
    headers,
    body: JSON.stringify({
      severity: notice.severity,
      source: notice.source,
      message: notice.message,
      method: notice.method ?? null,
      path: notice.path ?? null,
      status: notice.status ?? null,
      detail: notice.detail ?? null,
    }),
  }).catch(() => undefined);
}

function readDismissedToastIds(): Set<string> {
  try {
    const raw = window.sessionStorage.getItem(dismissedToastKey);
    if (!raw) {
      return new Set();
    }
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) {
      return new Set();
    }
    return new Set(parsed.filter((value): value is string => typeof value === "string"));
  } catch {
    return new Set();
  }
}

function persistDismissedToastIds(ids: Set<string>) {
  try {
    window.sessionStorage.setItem(
      dismissedToastKey,
      JSON.stringify([...ids].slice(-maxDismissedToasts)),
    );
  } catch {
    return;
  }
}

function markToastDismissed(id: string) {
  const ids = readDismissedToastIds();
  ids.add(id);
  persistDismissedToastIds(ids);
}

export function restoreIssueToast() {
  if (toast !== null) {
    return;
  }

  const dismissed = readDismissedToastIds();
  const now = Date.now();
  const pending = items.find((item) => {
    if (dismissed.has(item.id)) {
      return false;
    }
    const occurredAt = Date.parse(item.occurredAt);
    if (Number.isNaN(occurredAt)) {
      return false;
    }
    return now - occurredAt < restoreToastMs;
  });
  if (!pending) {
    return;
  }

  toast = pending;
  emit();
}

export function subscribeIssues(listener: Listener): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function getIssueNotices(): IssueNotice[] {
  return items;
}

export function getIssueToast(): IssueNotice | null {
  return toast;
}

export function dismissIssueToast() {
  if (toast) {
    markToastDismissed(toast.id);
  }
  toast = null;
  emit();
}

export function clearIssueNotices() {
  items = [];
  toast = null;
  persistDismissedToastIds(new Set());
  persist();
  emit();
}

export function formatIssueLog(notices: IssueNotice[]): string {
  if (notices.length === 0) {
    return "No Flexis issues recorded.";
  }

  return notices
    .map((notice) => {
      const lines = [
        `[${notice.severity}] ${notice.occurredAt}`,
        `source: ${notice.source}`,
        `message: ${notice.message}`,
      ];
      if (notice.method || notice.path) {
        lines.push(`request: ${[notice.method, notice.path].filter(Boolean).join(" ")}`);
      }
      if (notice.status !== undefined) {
        lines.push(`status: ${notice.status}`);
      }
      if (notice.detail) {
        lines.push(`detail: ${notice.detail}`);
      }
      return lines.join("\n");
    })
    .join("\n\n");
}

export function reportIssue(draft: IssueDraft) {
  const message = draft.message.trim();
  if (message.length === 0) {
    return;
  }

  const now = Date.now();
  const duplicate = items.find((item) => {
    return (
      item.severity === draft.severity &&
      item.source === draft.source &&
      item.message === message &&
      item.path === draft.path &&
      item.status === draft.status &&
      now - Date.parse(item.occurredAt) < dedupeMs
    );
  });
  if (duplicate) {
    const dismissed = readDismissedToastIds();
    dismissed.delete(duplicate.id);
    persistDismissedToastIds(dismissed);
    toast = duplicate;
    emit();
    return;
  }

  const notice: IssueNotice = {
    id: makeId(),
    occurredAt: new Date(now).toISOString(),
    severity: draft.severity,
    source: draft.source,
    message,
    method: draft.method,
    path: draft.path,
    status: draft.status,
    detail: draft.detail?.trim() ? draft.detail.trim() : undefined,
  };
  items = [notice, ...items].slice(0, maxItems);
  toast = notice;
  persist();
  emit();
  if (draft.path !== diagnosticsPath) {
    pushToServer(notice);
  }
}
