import type { MailCheckRunTiming } from "@/shared/types/mailCheck";

export type MailCheckRunStageId =
  | "lock"
  | "token"
  | "labels"
  | "scan"
  | "fetch"
  | "classify"
  | "apply"
  | "server";

export type MailCheckRunStage = {
  id: MailCheckRunStageId;
  label: string;
  description: string;
  timingKey: keyof MailCheckRunTiming | null;
};

export const mailCheckRunStages: MailCheckRunStage[] = [
  {
    id: "lock",
    label: "Wait for lock",
    description: "Only one classify run at a time. Manual Check cancels auto-check, then may wait briefly.",
    timingKey: "lockMs",
  },
  {
    id: "token",
    label: "Refresh token",
    description: "Get a fresh Gmail or Outlook access token for the mailbox",
    timingKey: "tokenMs",
  },
  {
    id: "labels",
    label: "Ensure labels",
    description: "List mailbox labels or categories and create missing Flexis ones",
    timingKey: "labelsMs",
  },
  {
    id: "scan",
    label: "Scan inbox",
    description: "Walk inbox pages to find the newest message without a Flexis label",
    timingKey: "scanMs",
  },
  {
    id: "fetch",
    label: "Fetch mail",
    description: "Download subject and body from Gmail or Outlook",
    timingKey: "fetchMs",
  },
  {
    id: "classify",
    label: "Classify",
    description: "OpenAI picks one label from your prompt",
    timingKey: "classifyMs",
  },
  {
    id: "apply",
    label: "Apply action",
    description: "Pin, trash, or keep, mark read, and save progress",
    timingKey: "applyMs",
  },
];

export function formatElapsedMs(ms: number): string {
  if (ms < 1000) {
    return `${ms} ms`;
  }

  const seconds = ms / 1000;
  if (seconds < 60) {
    return `${seconds.toFixed(1)} s`;
  }

  const minutes = Math.floor(seconds / 60);
  const remainder = Math.round(seconds % 60);
  return `${minutes}m ${remainder}s`;
}
