export type SheetRefreshKind = "auto" | "manual" | "workspace";

export type SheetRefreshProgress = (percent: number) => void;

export type SheetRefreshResult = "completed" | "skipped";

type SheetRefreshJob = {
  kind: SheetRefreshKind;
  run: (onProgress?: SheetRefreshProgress) => Promise<void>;
  onProgress?: SheetRefreshProgress;
  resolve: (result: SheetRefreshResult) => void;
  reject: (error: unknown) => void;
};

const kindPriority: Record<SheetRefreshKind, number> = {
  auto: 1,
  manual: 2,
  workspace: 3,
};

let active: SheetRefreshJob | null = null;
let pending: SheetRefreshJob | null = null;
let lastSuccessAt: number | null = null;
let lastKind: SheetRefreshKind | null = null;
const listeners = new Set<() => void>();

function notify(): void {
  for (const listener of listeners) {
    listener();
  }
}

export function getSheetRefreshSnapshot(): {
  isRunning: boolean;
  runningKind: SheetRefreshKind | null;
  lastSuccessAt: number | null;
  lastKind: SheetRefreshKind | null;
} {
  return {
    isRunning: active !== null,
    runningKind: active?.kind ?? null,
    lastSuccessAt,
    lastKind,
  };
}

export function subscribeSheetRefresh(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function markSheetRefreshSuccess(kind: SheetRefreshKind, at = Date.now()): void {
  lastSuccessAt = at;
  lastKind = kind;
  notify();
}

function shouldSkipAuto(skipIfFreshMs: number): boolean {
  if (active !== null || pending !== null) {
    return true;
  }

  if (lastSuccessAt === null) {
    return false;
  }

  return Date.now() - lastSuccessAt < skipIfFreshMs;
}

function replacePending(job: SheetRefreshJob): void {
  if (pending) {
    pending.resolve("skipped");
  }

  pending = job;
}

function takeNextJob(): SheetRefreshJob | null {
  if (!pending) {
    return null;
  }

  const next = pending;
  pending = null;
  return next;
}

async function runJob(job: SheetRefreshJob): Promise<void> {
  active = job;
  notify();
  try {
    await job.run(job.onProgress);
    markSheetRefreshSuccess(job.kind);
    job.resolve("completed");
  } catch (error) {
    job.reject(error);
  } finally {
    active = null;
    notify();
    const next = takeNextJob();
    if (next) {
      void runJob(next);
    }
  }
}

export async function requestSheetRefresh(
  kind: SheetRefreshKind,
  run: (onProgress?: SheetRefreshProgress) => Promise<void>,
  options?: {
    skipIfFreshMs?: number;
    onProgress?: SheetRefreshProgress;
  },
): Promise<SheetRefreshResult> {
  if (kind === "auto" && shouldSkipAuto(options?.skipIfFreshMs ?? 0)) {
    return "skipped";
  }

  return new Promise<SheetRefreshResult>((resolve, reject) => {
    const job: SheetRefreshJob = {
      kind,
      run,
      onProgress: options?.onProgress,
      resolve,
      reject,
    };

    if (!active) {
      void runJob(job);
      return;
    }

    if (kind === "auto") {
      resolve("skipped");
      return;
    }

    if (pending && kindPriority[pending.kind] > kindPriority[kind]) {
      resolve("skipped");
      return;
    }

    if (pending && kindPriority[pending.kind] === kindPriority[kind]) {
      pending.resolve("skipped");
      pending = job;
      return;
    }

    replacePending(job);
  });
}
