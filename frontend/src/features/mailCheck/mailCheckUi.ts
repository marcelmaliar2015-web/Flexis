import { userFacingError } from "@/shared/api/errors";
import type { MailCheckAction, MailCheckMailboxProvider } from "@/shared/types/mailCheck";

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
  keep: "Left in place",
  error: "Error",
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
  checkedUntilAt: string | null;
  scanCaughtUp: boolean;
}): string {
  if (mailbox.checkedUntilAt) {
    const until = `Checked until ${formatScanTimestamp(mailbox.checkedUntilAt)}`;
    return mailbox.scanCaughtUp ? `${until} · caught up` : until;
  }

  if (mailbox.scanCaughtUp) {
    return "Caught up";
  }

  return "Not checked yet";
}
