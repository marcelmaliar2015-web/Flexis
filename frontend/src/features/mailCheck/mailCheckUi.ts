import { userFacingError } from "@/shared/api/errors";
import type { MailCheckAction, MailCheckMailboxProvider, MailCheckRunTiming } from "@/shared/types/mailCheck";

export function errorMessage(error: unknown): string | null {
  return userFacingError(error);
}

export function mailboxMessageUrl(
  provider: MailCheckMailboxProvider | string,
  threadId: string,
  id: string,
): string {
  if (provider === "outlook") {
    return `https://outlook.office.com/mail/deeplink/read/${encodeURIComponent(id)}`;
  }

  const target = threadId.length > 0 ? threadId : id;
  return `https://mail.google.com/mail/u/0/#all/${target}`;
}

export function providerLabel(provider: MailCheckMailboxProvider | string): string {
  if (provider === "outlook") {
    return "Outlook";
  }
  if (provider === "gmail") {
    return "Gmail";
  }
  return "Mailbox";
}

export const mailCheckActionLabels: Record<string, string> = {
  pin: "Pinned",
  trash: "Trashed",
  keep: "Left in inbox",
  already_checked: "Already labeled",
  error: "Error",
  run_completed: "Run completed",
};

export function actionLabel(action: MailCheckAction | string): string {
  return mailCheckActionLabels[action] ?? action;
}

function formatScanTimestamp(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function formatMailboxScanStatus(mailbox: {
  checkedNewestAt: string | null;
  checkedUntilAt: string | null;
  scanCaughtUp: boolean;
}): string {
  const newest = mailbox.checkedNewestAt ?? null;
  const oldest = mailbox.checkedUntilAt ?? null;

  if (!newest && !oldest) {
    return mailbox.scanCaughtUp ? "Caught up" : "Not checked yet";
  }

  const parts: string[] = [];
  if (newest) {
    parts.push(`Latest classified ${formatScanTimestamp(newest)}`);
  }

  if (oldest) {
    parts.push(`Earliest in queue ${formatScanTimestamp(oldest)}`);
  }

  const status = parts.join(" · ");
  return mailbox.scanCaughtUp ? `${status} · caught up` : status;
}

function formatTimingMs(value: number): string {
  if (value < 1000) {
    return `${value} ms`;
  }

  return `${(value / 1000).toFixed(1)} s`;
}

export type MailCheckTimingRow = {
  label: string;
  ms: number;
  hint: string;
};

export function mailCheckTimingRows(timing: MailCheckRunTiming): MailCheckTimingRow[] {
  return [
    {
      label: "Wait for lock",
      ms: timing.lockMs,
      hint: "Time blocked because only one Mail Check run uses the server at a time",
    },
    {
      label: "Refresh token",
      ms: timing.tokenMs,
      hint: "OAuth access token for Gmail or Outlook",
    },
    {
      label: "Ensure labels",
      ms: timing.labelsMs,
      hint: "List and create Flexis labels or categories",
    },
    {
      label: "Scan",
      ms: timing.scanMs,
      hint: "Finding the next unprocessed message in Gmail or Outlook",
    },
    {
      label: "Fetch",
      ms: timing.fetchMs,
      hint: "Downloading subject and body from the mailbox",
    },
    {
      label: "Classify",
      ms: timing.classifyMs,
      hint: "OpenAI label request",
    },
    {
      label: "Apply",
      ms: timing.applyMs,
      hint: "Label, pin, trash, mark read, and save progress",
    },
  ];
}

export function formatMailCheckTimingSummary(timing: MailCheckRunTiming): string {
  if (timing.totalMs <= 0) {
    return "";
  }

  const rows = mailCheckTimingRows(timing)
    .filter((row) => row.ms > 0)
    .sort((left, right) => right.ms - left.ms)
    .map((row) => `${row.label} ${formatTimingMs(row.ms)}`);

  return `${formatTimingMs(timing.totalMs)} total · ${rows.join(" · ")}`;
}
