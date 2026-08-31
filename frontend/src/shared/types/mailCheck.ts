export type MailCheckLabelSlug =
  | "interviewScheduled"
  | "waitingForAnswer"
  | "needToSchedule"
  | "others";

export type MailCheckAction = MailCheckLabelSlug | "discard" | "skip" | "error";

export type MailCheckSettings = {
  hasApiKey: boolean;
  model: string;
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
  gmailConnected: boolean;
  googleEmail: string | null;
};

export type MailCheckSettingsWrite = {
  apiKey: string | null;
  clearApiKey: boolean;
  model: string;
};

export type MailCheckModel = {
  id: string;
  recommended: boolean;
};

export type MailCheckModels = {
  models: MailCheckModel[];
};

export type MailCheckRunItem = {
  gmailMessageId: string;
  subject: string;
  from: string;
  action: MailCheckAction | string;
  reason: string;
  label: string;
};

export type MailCheckRun = {
  busy: boolean;
  processed: number;
  labeled: number;
  trashed: number;
  skipped: number;
  errors: number;
  hasMore: boolean;
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
};

export type MailCheckInbox = {
  items: MailCheckInboxItem[];
};

export const mailCheckKeepLabels: { slug: MailCheckLabelSlug; name: string }[] = [
  { slug: "interviewScheduled", name: "Interview Scheduled" },
  { slug: "waitingForAnswer", name: "Waiting for answer" },
  { slug: "needToSchedule", name: "Need to Schedule/Availability" },
  { slug: "others", name: "Others" },
];
