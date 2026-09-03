import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";
import { isAbortError } from "@/shared/api/client";
import {
  getMailCheckRunProgress,
  mailCheckInboxRootQueryKey,
  mailCheckLastRunQueryKey,
  mailCheckLogsRootQueryKey,
  mailCheckNeedActionQueryKey,
  mailCheckSettingsQueryKey,
  runMailCheck,
} from "@/shared/api/mailCheck";
import type { MailCheckMailboxItem, MailCheckRunProgress } from "@/shared/types/mailCheck";
import { prepareMailCheckManualRun, setMailCheckManualRunActive } from "@/features/mailCheck/mailCheckRunCoordination";
import {
  clearMailCheckManualSession,
  loadMailCheckManualSession,
  saveMailCheckManualSession,
} from "@/features/mailCheck/mailCheckSessionStorage";
import {
  applySessionRound,
  createSession,
  sessionWaitingForServer,
  type MailCheckCheckSession,
} from "@/features/mailCheck/mailCheckRunSession";
import { errorMessage } from "@/features/mailCheck/mailCheckUi";

const maxRounds = 500;
const busyWaitMs = 1500;
const maxBusyRetries = 20;
const manualRunningKey = "flexis.mailCheck.manualRunning";

function wait(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    const id = window.setTimeout(resolve, ms);
    if (!signal) {
      return;
    }

    if (signal.aborted) {
      window.clearTimeout(id);
      reject(new DOMException("Aborted", "AbortError"));
      return;
    }

    signal.addEventListener(
      "abort",
      () => {
        window.clearTimeout(id);
        reject(new DOMException("Aborted", "AbortError"));
      },
      { once: true },
    );
  });
}

function publishProgress(
  onProgress: (session: MailCheckCheckSession) => void,
  session: MailCheckCheckSession,
): void {
  onProgress(session);
  saveMailCheckManualSession(session);
}

async function runUntilCaughtUp(
  mailboxes: MailCheckMailboxItem[],
  mailboxId: string | null,
  onProgress: (session: MailCheckCheckSession) => void,
  signal?: AbortSignal,
): Promise<MailCheckCheckSession> {
  const startedAt = Date.now();
  let session: MailCheckCheckSession = {
    ...createSession(mailboxes),
    startedAt,
    phase: "scanning",
    activeStage: "server",
    stageMessage: "Starting first server round",
    message: mailboxId ? "Starting mailbox check…" : "Starting check on all mailboxes…",
    serverWaitStartedAt: null,
    waitingForLock: false,
  };
  publishProgress(onProgress, session);

  let requestIndex = 0;
  let busyRetries = 0;

  try {
    while (requestIndex < maxRounds) {
      if (signal?.aborted) {
        break;
      }

      const mailboxEmail =
        mailboxId != null ? mailboxes.find((mailbox) => mailbox.id === mailboxId)?.email ?? null : null;
      session = {
        ...sessionWaitingForServer(session, mailboxId, mailboxEmail),
        serverWaitStartedAt: Date.now(),
        waitingForLock: false,
      };
      publishProgress(onProgress, session);

      const next = await runMailCheck({
        force: true,
        mailboxId,
        resetCursor: requestIndex === 0,
        signal,
      });
      requestIndex += 1;

      session = {
        ...applySessionRound(session, next, false, true),
        serverWaitStartedAt: null,
        waitingForLock: next.busy,
      };
      publishProgress(onProgress, session);

      if (next.busy) {
        busyRetries += 1;
        if (busyRetries > maxBusyRetries) {
          session = {
            ...session,
            phase: "cancelled",
            activeStage: "lock",
            stageMessage: "Gave up waiting for server lock",
            waitingForLock: false,
            message:
              "Could not get the server lock after repeated tries. Wait a moment, then click Check again.",
          };
          publishProgress(onProgress, session);
          return session;
        }

        await wait(busyWaitMs, signal);
        continue;
      }

      busyRetries = 0;
      if (!next.hasMore) {
        break;
      }
    }
  } catch (error) {
    if (!isAbortError(error)) {
      throw error;
    }
  }

  if (signal?.aborted) {
    session = {
      ...session,
      phase: "cancelled",
      activeStage: "idle",
      stageMessage: "Manual check cancelled",
      serverWaitStartedAt: null,
      waitingForLock: false,
      message:
        session.totals.processed > 0
          ? `Stopped after ${session.totals.processed} message${session.totals.processed === 1 ? "" : "s"}`
          : "Stopped before any messages were processed",
    };
  } else if (session.phase !== "done" && session.phase !== "cancelled") {
    session = {
      ...session,
      phase: "done",
      activeStage: "idle",
      stageMessage: "Manual check finished",
      waitingForLock: false,
      serverWaitStartedAt: null,
    };
  }

  publishProgress(onProgress, session);
  return session;
}

export type MailCheckRunTarget = string | "all" | null;

export function useMailCheckRun(mailboxes: MailCheckMailboxItem[]) {
  const queryClient = useQueryClient();
  const [session, setSession] = useState<MailCheckCheckSession | null>(null);
  const [runError, setRunError] = useState<string | null>(null);
  const [activeMailboxId, setActiveMailboxId] = useState<MailCheckRunTarget>(null);
  const [cancelling, setCancelling] = useState(false);
  const [elapsedMs, setElapsedMs] = useState(0);
  const [serverWaitMs, setServerWaitMs] = useState(0);
  const [liveProgress, setLiveProgress] = useState<MailCheckRunProgress | null>(null);
  const [interruptedByRefresh, setInterruptedByRefresh] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const cancelledRef = useRef(false);
  const restoredRef = useRef(false);

  useEffect(() => {
    if (restoredRef.current) {
      return;
    }

    restoredRef.current = true;
    const wasRunning = sessionStorage.getItem(manualRunningKey) === "true";
    if (wasRunning) {
      setInterruptedByRefresh(true);
      sessionStorage.removeItem(manualRunningKey);
    }

    const saved = loadMailCheckManualSession();
    if (saved) {
      setSession({
        ...saved,
        phase: wasRunning ? "cancelled" : saved.phase,
        activeStage: "idle",
        stageMessage: wasRunning
          ? "Interrupted by refresh — run Check again"
          : saved.stageMessage,
        message: wasRunning
          ? "Page refresh stopped the manual check UI. Auto-check may resume if enabled."
          : saved.message,
        serverWaitStartedAt: null,
        waitingForLock: false,
      });
    }
  }, []);

  const checkMutation = useMutation({
    mutationFn: (mailboxId: string | null) =>
      runUntilCaughtUp(mailboxes, mailboxId, setSession, abortRef.current?.signal),
    onMutate: (mailboxId) => {
      sessionStorage.setItem(manualRunningKey, "true");
      setInterruptedByRefresh(false);
      cancelledRef.current = false;
      setCancelling(false);
      setElapsedMs(0);
      setLiveProgress(null);
      abortRef.current = new AbortController();
      setRunError(null);
      setActiveMailboxId(mailboxId ?? "all");
    },
    onSettled: () => {
      setMailCheckManualRunActive(false);
      sessionStorage.removeItem(manualRunningKey);
    },
    onSuccess: async (result) => {
      setSession(result);
      saveMailCheckManualSession(result);
      queryClient.setQueryData(mailCheckLastRunQueryKey, result);
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckInboxRootQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckNeedActionQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckLogsRootQueryKey });
      setActiveMailboxId(null);
      abortRef.current = null;
      setCancelling(false);
    },
    onError: (error) => {
      abortRef.current = null;
      setActiveMailboxId(null);
      setCancelling(false);
      sessionStorage.removeItem(manualRunningKey);
      if (isAbortError(error)) {
        return;
      }

      setRunError(errorMessage(error));
      setSession(null);
      clearMailCheckManualSession();
      queryClient.setQueryData(mailCheckLastRunQueryKey, null);
    },
  });

  const cancelCheck = useCallback(() => {
    cancelledRef.current = true;
    setCancelling(true);
    abortRef.current?.abort();
  }, []);

  const checking = checkMutation.isPending;

  useEffect(() => {
    if (!checking || !session?.startedAt) {
      return;
    }

    const startedAt = session.startedAt;
    const tick = () => setElapsedMs(Date.now() - startedAt);
    tick();
    const id = window.setInterval(tick, 500);
    return () => window.clearInterval(id);
  }, [checking, session?.startedAt]);

  useEffect(() => {
    if (!checking || !session?.serverWaitStartedAt) {
      setServerWaitMs(0);
      return;
    }

    const startedAt = session.serverWaitStartedAt;
    const tick = () => setServerWaitMs(Date.now() - startedAt);
    tick();
    const id = window.setInterval(tick, 500);
    return () => window.clearInterval(id);
  }, [checking, session?.serverWaitStartedAt]);

  useEffect(() => {
    if (!checking) {
      setLiveProgress(null);
      return;
    }

    let cancelled = false;

    const poll = async () => {
      try {
        const progress = await getMailCheckRunProgress();
        if (!cancelled) {
          setLiveProgress(progress);
        }
      } catch {
      }
    };

    void poll();
    const id = window.setInterval(() => {
      void poll();
    }, 750);

    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, [checking]);

  return {
    session,
    runError,
    activeMailboxId,
    cancelling,
    checking,
    elapsedMs,
    serverWaitMs,
    liveProgress,
    interruptedByRefresh,
    cancelCheck,
    startCheck: (mailboxId: string | null) => {
      prepareMailCheckManualRun();
      checkMutation.mutate(mailboxId);
    },
  };
}
