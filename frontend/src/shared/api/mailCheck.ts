import { deleteRequest, getJson, postJson, putJson } from "@/shared/api/client";
import type { QueryClient } from "@tanstack/react-query";
import type {
  MailCheckActionLogPage,
  MailCheckActionLogQuery,
  MailCheckInbox,
  MailCheckLabelSlug,
  MailCheckMailboxStatus,
  MailCheckModels,
  MailCheckRun,
  MailCheckRunProgress,
  MailCheckSettings,
  MailCheckSettingsWrite,
} from "@/shared/types/mailCheck";
export const mailCheckSettingsQueryKey = ["mail-check-settings"] as const;

export const mailCheckMailboxQueryKey = ["mail-check-mailbox"] as const;

export const mailCheckModelsQueryKey = ["mail-check-models"] as const;

export const mailCheckLastRunQueryKey = ["mail-check-last-run"] as const;

export const mailCheckRunProgressQueryKey = ["mail-check-run-progress"] as const;

export const mailCheckNeedActionQueryKey = ["mail-check-need-action"] as const;

export const mailCheckInboxRootQueryKey = ["mail-check-inbox"] as const;

export const mailCheckLogsRootQueryKey = ["mail-check-logs"] as const;

export const mailCheckInboxQueryKey = (label: MailCheckLabelSlug | "all") =>
  [...mailCheckInboxRootQueryKey, label] as const;

export const mailCheckLogsQueryKey = (query: MailCheckActionLogQuery) =>
  [
    ...mailCheckLogsRootQueryKey,
    query.page ?? 1,
    query.pageSize ?? 50,
    query.source ?? "all",
    query.action ?? "all",
    query.mailboxId ?? "all",
    query.q ?? "",
  ] as const;

export const mailCheckSettingsRevisionQueryKey = ["mail-check-settings-revision"] as const;

export const defaultAutoCheckIntervalSeconds = 20;

export function mailCheckAutoIntervalMs(intervalSeconds: number): number {
  return Math.max(intervalSeconds, 1) * 1000;
}

export function bumpMailCheckSettingsRevision(queryClient: QueryClient): void {
  const current = queryClient.getQueryData<number>(mailCheckSettingsRevisionQueryKey) ?? 0;
  queryClient.setQueryData(mailCheckSettingsRevisionQueryKey, current + 1);
}

export function getMailCheckSettings(): Promise<MailCheckSettings> {
  return getJson<MailCheckSettings>("/api/mail-check/settings");
}

export function updateMailCheckSettings(
  request: MailCheckSettingsWrite,
): Promise<MailCheckSettings> {
  return putJson<MailCheckSettings>("/api/mail-check/settings", request);
}

export function listMailCheckModels(): Promise<MailCheckModels> {
  return getJson<MailCheckModels>("/api/mail-check/models");
}

export function runMailCheck(options?: {
  force?: boolean;
  mailboxId?: string | null;
  resetCursor?: boolean;
  signal?: AbortSignal;
}): Promise<MailCheckRun> {
  return postJson<MailCheckRun>(
    "/api/mail-check/run",
    {
      force: options?.force ?? false,
      mailboxId: options?.mailboxId ?? null,
      resetCursor: options?.resetCursor ?? false,
    },
    { signal: options?.signal },
  );
}

export function getMailCheckRunProgress(): Promise<MailCheckRunProgress> {
  return getJson<MailCheckRunProgress>("/api/mail-check/run/progress");
}

export function getMailCheckInbox(label: MailCheckLabelSlug | "all"): Promise<MailCheckInbox> {
  const query = label === "all" ? "" : `?label=${encodeURIComponent(label)}`;
  return getJson<MailCheckInbox>(`/api/mail-check/inbox${query}`);
}

export function getMailCheckNeedAction(): Promise<MailCheckInbox> {
  return getJson<MailCheckInbox>("/api/mail-check/need-action");
}

export function listMailCheckLogs(query: MailCheckActionLogQuery): Promise<MailCheckActionLogPage> {
  const params = new URLSearchParams();
  params.set("page", String(query.page ?? 1));
  params.set("pageSize", String(query.pageSize ?? 50));
  if (query.source && query.source !== "all") {
    params.set("source", query.source);
  }
  if (query.action && query.action !== "all") {
    params.set("action", query.action);
  }
  if (query.mailboxId) {
    params.set("mailboxId", query.mailboxId);
  }
  if (query.q?.trim()) {
    params.set("q", query.q.trim());
  }
  return getJson<MailCheckActionLogPage>(`/api/mail-check/logs?${params.toString()}`);
}

export function getMailCheckMailbox(): Promise<MailCheckMailboxStatus> {
  return getJson<MailCheckMailboxStatus>("/api/mail-check/mailbox");
}

export function startMailCheckGmail(returnUrl: string): Promise<{ authorizationUrl: string }> {
  return postJson<{ authorizationUrl: string }>("/api/mail-check/mailbox/gmail/start", { returnUrl });
}

export function startMailCheckOutlook(returnUrl: string): Promise<{ authorizationUrl: string }> {
  return postJson<{ authorizationUrl: string }>("/api/mail-check/mailbox/outlook/start", { returnUrl });
}

export function disconnectMailCheckMailbox(id: string): Promise<void> {
  return deleteRequest(`/api/mail-check/mailbox/${id}`);
}
