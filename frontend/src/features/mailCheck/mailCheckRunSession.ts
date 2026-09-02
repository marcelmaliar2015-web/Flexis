import type { MailCheckMailboxItem, MailCheckRun } from "@/shared/types/mailCheck";

export type MailCheckCheckPhase =
  | "idle"
  | "scanning"
  | "classifying"
  | "waiting"
  | "done"
  | "cancelled";

export type MailboxCheckStats = {
  mailboxId: string;
  email: string;
  provider: MailCheckMailboxItem["provider"] | string;
  processed: number;
  labeled: number;
  trashed: number;
  skipped: number;
  errors: number;
};

export type MailCheckCheckSession = {
  totals: MailCheckRun;
  mailboxStats: Record<string, MailboxCheckStats>;
  activeMailboxId: string | null;
  round: number;
  phase: MailCheckCheckPhase;
  message: string | null;
};

export function emptyRun(): MailCheckRun {
  return {
    busy: false,
    processed: 0,
    labeled: 0,
    trashed: 0,
    skipped: 0,
    errors: 0,
    hasMore: false,
    scanned: 0,
    alreadySeen: 0,
    mailboxId: null,
    mailboxEmail: null,
    mailboxProvider: null,
    items: [],
  };
}

export function createSession(mailboxes: MailCheckMailboxItem[]): MailCheckCheckSession {
  const mailboxStats: Record<string, MailboxCheckStats> = {};
  for (const mailbox of mailboxes) {
    mailboxStats[mailbox.id] = {
      mailboxId: mailbox.id,
      email: mailbox.email,
      provider: mailbox.provider,
      processed: 0,
      labeled: 0,
      trashed: 0,
      skipped: 0,
      errors: 0,
    };
  }

  return {
    totals: emptyRun(),
    mailboxStats,
    activeMailboxId: null,
    round: 0,
    phase: "idle",
    message: null,
  };
}

export function mergeRuns(previous: MailCheckRun, next: MailCheckRun): MailCheckRun {
  return {
    ...next,
    processed: previous.processed + next.processed,
    labeled: previous.labeled + next.labeled,
    trashed: previous.trashed + next.trashed,
    skipped: previous.skipped + next.skipped,
    errors: previous.errors + next.errors,
    scanned: previous.scanned + next.scanned,
    alreadySeen: previous.alreadySeen + next.alreadySeen,
    items: [...previous.items, ...next.items],
  };
}

function phaseFromRound(next: MailCheckRun): MailCheckCheckPhase {
  if (next.busy) {
    return "waiting";
  }

  if (next.processed > 0) {
    return "classifying";
  }

  if (next.scanned > 0) {
    return "scanning";
  }

  return "idle";
}

function statusMessage(
  phase: MailCheckCheckPhase,
  totals: MailCheckRun,
  round: number,
  mailboxEmail: string | null,
): string {
  if (phase === "waiting") {
    return "Waiting for the current mailbox check to finish…";
  }

  if (phase === "scanning" && mailboxEmail) {
    return `Scanning ${mailboxEmail}`;
  }

  if (phase === "classifying" && mailboxEmail) {
    return `Classifying message ${round} in ${mailboxEmail}`;
  }

  if (totals.processed > 0) {
    return `Processed ${totals.processed} message${totals.processed === 1 ? "" : "s"}`;
  }

  return "Starting check…";
}

function applyMailboxRound(
  mailboxStats: Record<string, MailboxCheckStats>,
  next: MailCheckRun,
): void {
  if (!next.mailboxId) {
    return;
  }

  const current = mailboxStats[next.mailboxId];
  if (!current) {
    return;
  }

  mailboxStats[next.mailboxId] = {
    ...current,
    processed: current.processed + next.processed,
    labeled: current.labeled + next.labeled,
    trashed: current.trashed + next.trashed,
    skipped: current.skipped + next.skipped,
    errors: current.errors + next.errors,
  };
}

export function applySessionRound(
  session: MailCheckCheckSession,
  next: MailCheckRun,
  cancelled: boolean,
): MailCheckCheckSession {
  const totals = mergeRuns(session.totals, next);
  const mailboxStats = { ...session.mailboxStats };
  applyMailboxRound(mailboxStats, next);

  const round = next.busy ? session.round : session.round + (next.processed > 0 ? 1 : 0);
  let phase = phaseFromRound(next);
  if (cancelled) {
    phase = "cancelled";
  } else if (!next.hasMore && !next.busy) {
    phase = "done";
  }
  const activeMailboxId = next.mailboxId ?? session.activeMailboxId;
  const mailboxEmail = next.mailboxEmail ?? null;

  let message = statusMessage(phase, totals, round, mailboxEmail);
  if (cancelled) {
    message =
      totals.processed > 0
        ? `Stopped after ${totals.processed} message${totals.processed === 1 ? "" : "s"}`
        : "Stopped before any messages were processed";
  } else if (!next.hasMore && !next.busy) {
    if (totals.processed === 0 && totals.scanned === 0) {
      message = "No candidate mail found in the connected mailboxes.";
    } else if (totals.processed === 0) {
      message = `Scanned ${totals.scanned} candidate${totals.scanned === 1 ? "" : "s"}; nothing new to classify.`;
    } else {
      message = `Finished · ${totals.processed} message${totals.processed === 1 ? "" : "s"} processed`;
    }
  } else if (next.busy && totals.processed === 0) {
    message = "Another check is still running. Wait a moment and try again.";
  }

  return {
    totals,
    mailboxStats,
    activeMailboxId,
    round,
    phase,
    message,
  };
}
