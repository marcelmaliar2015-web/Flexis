export type MailCheckMailboxProvider = "gmail" | "outlook";

export type MailCheckLabelSlug =
  | "rejected"
  | "applied"
  | "schedule"
  | "scheduled"
  | "assessment"
  | "availability"
  | "success"
  | "other"
  | "less_important";

export type MailCheckMailboxAction = "pin" | "trash" | "keep";

export type MailCheckAction = MailCheckMailboxAction | "already_checked" | "error";

export type MailCheckMailboxItem = {
  id: string;
  provider: MailCheckMailboxProvider;
  email: string;
  connectedAt: string;
  checkedNewestAt: string | null;
  checkedUntilAt: string | null;
  lastScanAt: string | null;
  scanCaughtUp: boolean;
};

export type MailCheckSettings = {
  hasApiKey: boolean;
  model: string;
  classifierPrompt: string;
  defaultClassifierPrompt: string;
  labelActions: Record<MailCheckLabelSlug, MailCheckMailboxAction>;
  defaultLabelActions: Record<MailCheckLabelSlug, MailCheckMailboxAction>;
  needActionLabels: MailCheckLabelSlug[];
  defaultNeedActionLabels: MailCheckLabelSlug[];
  autoCheckEnabled: boolean;
  autoCheckIntervalSeconds: number;
  lastRunAt: string | null;
  lastError: string;
  lastLabeled: number;
  lastTrashed: number;
  lastSkipped: number;
  lastProcessed: number;
  lastErrors: number;
  lastHasMore: boolean;
  totalLabeled: number;
  totalTrashed: number;
  mailboxes: MailCheckMailboxItem[];
  outlookAvailable: boolean;
};

export type MailCheckMailboxStatus = {
  outlookAvailable: boolean;
  mailboxes: MailCheckMailboxItem[];
};

export type MailCheckSettingsWrite = {
  apiKey: string | null;
  clearApiKey: boolean;
  model: string;
  classifierPrompt?: string | null;
  labelActions?: Record<MailCheckLabelSlug, MailCheckMailboxAction> | null;
  needActionLabels?: MailCheckLabelSlug[] | null;
  autoCheckEnabled?: boolean | null;
};

export type MailCheckModel = {
  id: string;
  recommended: boolean;
};

export type MailCheckModels = {
  models: MailCheckModel[];
};

export type MailCheckRunItem = {
  messageId: string;
  subject: string;
  from: string;
  action: MailCheckAction | string;
  reason: string;
  label: string;
  mailboxId: string;
  mailboxEmail: string;
  mailboxProvider: MailCheckMailboxProvider | string;
};

export type MailCheckRunTiming = {
  totalMs: number;
  lockMs: number;
  tokenMs: number;
  labelsMs: number;
  scanMs: number;
  fetchMs: number;
  classifyMs: number;
  applyMs: number;
};

export type MailCheckRunProgress = {
  active: boolean;
  stage: string;
  message: string;
  mailboxEmail: string | null;
  processed: number;
  scanned: number;
  alreadySeen: number;
  scanPage: number;
  elapsedMs: number;
  waitingForLock: boolean;
  activeRunKind: string | null;
  waitingRequestKind: string | null;
};

export type MailCheckRun = {
  busy: boolean;
  processed: number;
  labeled: number;
  trashed: number;
  skipped: number;
  errors: number;
  hasMore: boolean;
  scanned: number;
  alreadySeen: number;
  mailboxId: string | null;
  mailboxEmail: string | null;
  mailboxProvider: MailCheckMailboxProvider | string | null;
  items: MailCheckRunItem[];
  timing: MailCheckRunTiming;
};

export type MailCheckInboxItem = {
  id: string;
  threadId: string;
  subject: string;
  from: string;
  date: string;
  snippet: string;
  label: string;
  labelSlug: MailCheckLabelSlug | string;
  starred: boolean;
  mailboxId: string;
  mailboxEmail: string;
  mailboxProvider: MailCheckMailboxProvider | string;
};

export type MailCheckInbox = {
  items: MailCheckInboxItem[];
};

export const mailCheckLabels: { slug: MailCheckLabelSlug; name: string }[] = [
  { slug: "rejected", name: "Rejected" },
  { slug: "applied", name: "Applied" },
  { slug: "schedule", name: "Schedule" },
  { slug: "scheduled", name: "Scheduled" },
  { slug: "assessment", name: "Assessment" },
  { slug: "availability", name: "Availability" },
  { slug: "success", name: "Success" },
  { slug: "other", name: "Other" },
  { slug: "less_important", name: "Less Important" },
];

export const mailCheckMailboxActions: { value: MailCheckMailboxAction; label: string }[] = [
  { value: "pin", label: "Pin" },
  { value: "trash", label: "Trash" },
  { value: "keep", label: "Keep" },
];

export function mailCheckPinLabelFilters(
  labelActions: Record<MailCheckLabelSlug, MailCheckMailboxAction> | undefined,
): { slug: MailCheckLabelSlug; name: string }[] {
  if (!labelActions) {
    return mailCheckLabels.filter((item) =>
      ["schedule", "scheduled", "assessment", "availability", "success"].includes(item.slug),
    );
  }

  return mailCheckLabels.filter((item) => labelActions[item.slug] === "pin");
}
