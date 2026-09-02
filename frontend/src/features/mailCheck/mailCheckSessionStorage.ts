import {
  emptyRun,
  emptyTiming,
  type MailCheckCheckSession,
} from "@/features/mailCheck/mailCheckRunSession";

const storageKey = "flexis.mailCheck.manualSession";

export function saveMailCheckManualSession(session: MailCheckCheckSession): void {
  try {
    sessionStorage.setItem(storageKey, JSON.stringify(session));
  } catch {
  }
}

export function loadMailCheckManualSession(): MailCheckCheckSession | null {
  try {
    const raw = sessionStorage.getItem(storageKey);
    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw) as MailCheckCheckSession;
    return {
      ...parsed,
      serverWaitStartedAt: parsed.serverWaitStartedAt ?? null,
      waitingForLock: parsed.waitingForLock ?? false,
      totals: {
        ...emptyRun(),
        ...parsed.totals,
        timing: {
          ...emptyTiming(),
          ...parsed.totals?.timing,
        },
      },
    };
  } catch {
    return null;
  }
}

export function clearMailCheckManualSession(): void {
  try {
    sessionStorage.removeItem(storageKey);
  } catch {
  }
}
