import { userFacingError } from "@/shared/api/errors";
import type { MailCheckAction } from "@/shared/types/mailCheck";
export function errorMessage(error: unknown): string | null {
  return userFacingError(error);
}

export function gmailMessageUrl(threadId: string, id: string): string {
  const target = threadId.length > 0 ? threadId : id;
  return `https://mail.google.com/mail/u/0/#all/${target}`;
}

export const mailCheckActionLabels: Record<string, string> = {
  interviewScheduled: "Interview Scheduled",
  waitingForAnswer: "Waiting for answer",
  needToSchedule: "Need to Schedule/Availability",
  others: "Others",
  discard: "Trashed",
  skip: "Left in place",
  error: "Error",
};

export function actionLabel(action: MailCheckAction | string): string {
  return mailCheckActionLabels[action] ?? action;
}
