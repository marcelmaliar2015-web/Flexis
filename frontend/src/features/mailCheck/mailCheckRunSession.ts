import { mailCheckRunStages, type MailCheckRunStageId } from "@/features/mailCheck/mailCheckStages";
import type { MailCheckMailboxItem, MailCheckRun, MailCheckRunTiming } from "@/shared/types/mailCheck";

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
  alreadySeen: number;
  errors: number;
};

export type MailCheckCheckSession = {
  totals: MailCheckRun;
  mailboxStats: Record<string, MailboxCheckStats>;
  activeMailboxId: string | null;
  round: number;
  phase: MailCheckCheckPhase;
  message: string | null;
  activeStage: MailCheckRunStageId | "server" | "idle";
  stageMessage: string | null;
  startedAt: number | null;
  serverWaitStartedAt: number | null;
  waitingForLock: boolean;
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
    timing: emptyTiming(),
  };
}

export function emptyTiming(): MailCheckRunTiming {
  return {
    totalMs: 0,
    lockMs: 0,
    tokenMs: 0,
    labelsMs: 0,
    scanMs: 0,
    fetchMs: 0,
    classifyMs: 0,
    applyMs: 0,
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
      alreadySeen: 0,
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
    activeStage: "idle",
    stageMessage: null,
    startedAt: null,
    serverWaitStartedAt: null,
    waitingForLock: false,
  };
}

export function mergeRuns(previous: MailCheckRun, next: MailCheckRun): MailCheckRun {
  const previousTiming = previous.timing ?? emptyTiming();
  const nextTiming = next.timing ?? emptyTiming();
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
    timing: {
      totalMs: previousTiming.totalMs + nextTiming.totalMs,
      lockMs: previousTiming.lockMs + nextTiming.lockMs,
      tokenMs: previousTiming.tokenMs + nextTiming.tokenMs,
      labelsMs: previousTiming.labelsMs + nextTiming.labelsMs,
      scanMs: previousTiming.scanMs + nextTiming.scanMs,
      fetchMs: previousTiming.fetchMs + nextTiming.fetchMs,
      classifyMs: previousTiming.classifyMs + nextTiming.classifyMs,
      applyMs: previousTiming.applyMs + nextTiming.applyMs,
    },
  };
}

function timingDelta(previous: MailCheckRunTiming, next: MailCheckRunTiming): MailCheckRunTiming {
  return {
    totalMs: next.totalMs - previous.totalMs,
    lockMs: next.lockMs - previous.lockMs,
    tokenMs: next.tokenMs - previous.tokenMs,
    labelsMs: next.labelsMs - previous.labelsMs,
    scanMs: next.scanMs - previous.scanMs,
    fetchMs: next.fetchMs - previous.fetchMs,
    classifyMs: next.classifyMs - previous.classifyMs,
    applyMs: next.applyMs - previous.applyMs,
  };
}

function dominantStage(delta: MailCheckRunTiming): MailCheckRunStageId {
  const ranked: { id: MailCheckRunStageId; ms: number }[] = [
    { id: "lock", ms: delta.lockMs },
    { id: "token", ms: delta.tokenMs },
    { id: "labels", ms: delta.labelsMs },
    { id: "scan", ms: delta.scanMs },
    { id: "fetch", ms: delta.fetchMs },
    { id: "classify", ms: delta.classifyMs },
    { id: "apply", ms: delta.applyMs },
  ];
  ranked.sort((left, right) => right.ms - left.ms);
  return ranked[0]?.ms > 0 ? ranked[0].id : "scan";
}

function phaseFromRound(next: MailCheckRun, checking: boolean): MailCheckCheckPhase {
  if (next.busy) {
    return "waiting";
  }

  if (checking) {
    if (next.processed > 0) {
      return "classifying";
    }

    if (next.scanned > 0) {
      return "scanning";
    }

    return "scanning";
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
    return "Waiting for server lock — auto-check or another run is active";
  }

  if (phase === "scanning" && mailboxEmail) {
    return `Scanning ${mailboxEmail} for the next unprocessed message`;
  }

  if (phase === "classifying" && mailboxEmail) {
    return `Classified ${round} message${round === 1 ? "" : "s"} in ${mailboxEmail}`;
  }

  if (totals.processed > 0) {
    return `Processed ${totals.processed} message${totals.processed === 1 ? "" : "s"}`;
  }

  return "Starting manual check…";
}

function stageLabel(stageId: MailCheckRunStageId): string {
  return mailCheckRunStages.find((stage) => stage.id === stageId)?.label ?? stageId;
}

function stageMessageForRound(
  delta: MailCheckRunTiming,
  mailboxEmail: string | null,
  processed: number,
): string {
  const stage = dominantStage(delta);
  const target = mailboxEmail ?? "mailbox";
  if (processed > 0) {
    return `Last round on ${target}: ${stageLabel(stage)} took the most time`;
  }

  if (delta.scanMs > 0 && delta.classifyMs === 0) {
    return `Scanned inbox on ${target}; still looking for a message to classify`;
  }

  if (delta.lockMs > 0 && delta.tokenMs === 0 && delta.labelsMs === 0 && delta.scanMs === 0) {
    return `Last round on ${target}: waited for server lock`;
  }

  return `Finished server round on ${target}`;
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
    alreadySeen: current.alreadySeen + next.alreadySeen,
    errors: current.errors + next.errors,
  };
}

export function sessionWaitingForServer(
  session: MailCheckCheckSession,
  mailboxId: string | null,
  mailboxEmail: string | null,
): MailCheckCheckSession {
  const target = mailboxEmail ?? (mailboxId ? "selected mailbox" : "all mailboxes");
  return {
    ...session,
    activeStage: "server",
    stageMessage:
      "Server round in progress — lock, token, labels, scan, fetch, classify, apply run inside one request",
    message: `Server working on ${target}…`,
    phase: session.phase === "idle" ? "scanning" : session.phase,
    waitingForLock: false,
  };
}

export function applySessionRound(
  session: MailCheckCheckSession,
  next: MailCheckRun,
  cancelled: boolean,
  checking = true,
): MailCheckCheckSession {
  const previousTiming = session.totals.timing ?? emptyTiming();
  const totals = mergeRuns(session.totals, next);
  const delta = timingDelta(previousTiming, totals.timing);
  const mailboxStats = { ...session.mailboxStats };
  applyMailboxRound(mailboxStats, next);

  const round = next.busy ? session.round : session.round + (next.processed > 0 ? 1 : 0);
  let phase = phaseFromRound(next, checking);
  if (cancelled) {
    phase = "cancelled";
  } else if (!next.hasMore && !next.busy && checking) {
    phase = "done";
  } else if (!next.hasMore && !next.busy) {
    phase = "done";
  }

  const activeMailboxId = next.mailboxId ?? session.activeMailboxId;
  const mailboxEmail = next.mailboxEmail ?? null;

  let message = statusMessage(phase, totals, round, mailboxEmail);
  let activeStage: MailCheckCheckSession["activeStage"] = dominantStage(delta);
  let stageMessage = stageMessageForRound(delta, mailboxEmail, next.processed);

  if (cancelled) {
    message =
      totals.processed > 0
        ? `Stopped after ${totals.processed} message${totals.processed === 1 ? "" : "s"}`
        : "Stopped before any messages were processed";
    activeStage = "idle";
    stageMessage = "Manual check cancelled";
  } else if (!next.hasMore && !next.busy) {
    if (totals.processed === 0 && totals.scanned === 0) {
      message = "No candidate mail found in the connected mailboxes.";
    } else if (totals.processed === 0) {
      message = `Scanned ${totals.scanned} candidate${totals.scanned === 1 ? "" : "s"}; nothing new to classify.`;
    } else {
      message = `Finished · ${totals.processed} message${totals.processed === 1 ? "" : "s"} processed`;
    }
    activeStage = "idle";
    stageMessage = "Manual check finished";
  } else if (next.busy) {
    message = "Waiting for server lock — auto-check or another run is active";
    activeStage = "lock";
    stageMessage = "Another Mail Check run holds the server lock";
  } else if (next.hasMore && checking) {
    activeStage = "server";
    stageMessage = "More mail waiting — starting next server round";
  }

  return {
    totals,
    mailboxStats,
    activeMailboxId,
    round,
    phase,
    message,
    activeStage,
    stageMessage,
    startedAt: session.startedAt,
    serverWaitStartedAt: session.serverWaitStartedAt,
    waitingForLock: next.busy,
  };
}
