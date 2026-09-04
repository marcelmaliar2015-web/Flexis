import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { googleSyncIntervalMs, syncGoogleWorkspace } from "@/shared/api/googleSync";
import {
  getSheetRefreshSnapshot,
  subscribeSheetRefresh,
} from "@/shared/api/sheetRefreshCoordinator";
import { useAuth } from "@/shared/auth/AuthProvider";

type GoogleSyncContextValue = {
  lastSyncedAt: number | null;
  failed: boolean;
  isSyncing: boolean;
  syncProgress: number;
  refresh: () => Promise<void>;
};

const GoogleSyncContext = createContext<GoogleSyncContextValue | null>(null);

type GoogleSyncProviderProps = {
  children: ReactNode;
};

export function GoogleSyncProvider({ children }: GoogleSyncProviderProps) {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const [lastSyncedAt, setLastSyncedAt] = useState<number | null>(null);
  const [failed, setFailed] = useState(false);
  const [isSyncing, setIsSyncing] = useState(false);
  const [syncProgress, setSyncProgress] = useState(0);

  useEffect(() => {
    return subscribeSheetRefresh(() => {
      const snapshot = getSheetRefreshSnapshot();
      setLastSyncedAt(snapshot.lastSuccessAt);
      setIsSyncing(snapshot.isRunning);
      if (!snapshot.isRunning) {
        setSyncProgress(0);
      }
    });
  }, []);

  const refresh = useCallback(async () => {
    if (!auth.user) {
      return;
    }

    setFailed(false);
    setSyncProgress(0);
    setIsSyncing(true);
    try {
      const result = await syncGoogleWorkspace(queryClient, "manual", setSyncProgress);
      if (result === "skipped") {
        setIsSyncing(getSheetRefreshSnapshot().isRunning);
      }
    } catch {
      setFailed(true);
      setIsSyncing(false);
      setSyncProgress(0);
    }
  }, [auth.user, queryClient]);

  useEffect(() => {
    if (!auth.user) {
      setLastSyncedAt(null);
      setFailed(false);
      setIsSyncing(false);
      setSyncProgress(0);
      return;
    }

    async function runAuto() {
      if (document.visibilityState !== "visible") {
        return;
      }

      setFailed(false);
      try {
        await syncGoogleWorkspace(queryClient, "auto", setSyncProgress);
      } catch {
        setFailed(true);
        setIsSyncing(false);
        setSyncProgress(0);
      }
    }

    void runAuto();
    const timer = window.setInterval(() => {
      void runAuto();
    }, googleSyncIntervalMs);

    function onVisible() {
      void runAuto();
    }

    document.addEventListener("visibilitychange", onVisible);
    return () => {
      window.clearInterval(timer);
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [auth.user, queryClient]);

  const value = useMemo(
    () => ({ lastSyncedAt, failed, isSyncing, syncProgress, refresh }),
    [failed, isSyncing, lastSyncedAt, refresh, syncProgress],
  );

  return <GoogleSyncContext.Provider value={value}>{children}</GoogleSyncContext.Provider>;
}

export function useGoogleSync(): GoogleSyncContextValue {
  const value = useContext(GoogleSyncContext);
  if (!value) {
    throw new Error("useGoogleSync must be used within GoogleSyncProvider.");
  }

  return value;
}
