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
  interviewSchedule: "Interview Schedule",
  availabilityRequest: "Availability Request",
  assessmentRequest: "Assessment Request",
  hrTeamMessage: "HR Team Message",
  replyRequired: "Reply required",
  discard: "Trashed",
  skip: "Left in place",
  error: "Error",
};

export function actionLabel(action: MailCheckAction | string): string {
  return mailCheckActionLabels[action] ?? action;
}
