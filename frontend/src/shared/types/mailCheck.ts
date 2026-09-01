export type MailCheckMailboxProvider = "gmail" | "outlook";

export type MailCheckLabelSlug =
  | "interviewSchedule"
  | "availabilityRequest"
  | "assessmentRequest"
  | "hrTeamMessage"
  | "replyRequired";

export type MailCheckAction = MailCheckLabelSlug | "discard" | "skip" | "error";

export type MailCheckMailboxItem = {
  id: string;
  provider: MailCheckMailboxProvider;
  email: string;
  connectedAt: string;
};

export type MailCheckSettings = {
  hasApiKey: boolean;
  model: string;
  classifierPrompt: string;
  defaultClassifierPrompt: string;
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
  items: MailCheckRunItem[];
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

export const mailCheckKeepLabels: { slug: MailCheckLabelSlug; name: string }[] = [
  { slug: "interviewSchedule", name: "Interview Schedule" },
  { slug: "availabilityRequest", name: "Availability Request" },
  { slug: "assessmentRequest", name: "Assessment Request" },
  { slug: "hrTeamMessage", name: "HR Team Message" },
  { slug: "replyRequired", name: "Reply required" },
];
