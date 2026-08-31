import { getJson, postJson, putJson } from "@/shared/api/client";
import type {
  MailCheckInbox,
  MailCheckLabelSlug,
  MailCheckModels,
  MailCheckRun,
  MailCheckSettings,
  MailCheckSettingsWrite,
} from "@/shared/types/mailCheck";

export const mailCheckSettingsQueryKey = ["mail-check-settings"] as const;

export const mailCheckModelsQueryKey = ["mail-check-models"] as const;

export const mailCheckLastRunQueryKey = ["mail-check-last-run"] as const;

export const mailCheckInboxRootQueryKey = ["mail-check-inbox"] as const;

export const mailCheckInboxQueryKey = (label: MailCheckLabelSlug | "all") =>
  [...mailCheckInboxRootQueryKey, label] as const;

export const mailCheckIntervalMs = 120_000;

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

export function runMailCheck(force: boolean): Promise<MailCheckRun> {
  return postJson<MailCheckRun>("/api/mail-check/run", { force });
}

export function getMailCheckInbox(label: MailCheckLabelSlug | "all"): Promise<MailCheckInbox> {
  const query = label === "all" ? "" : `?label=${encodeURIComponent(label)}`;
  return getJson<MailCheckInbox>(`/api/mail-check/inbox${query}`);
}
