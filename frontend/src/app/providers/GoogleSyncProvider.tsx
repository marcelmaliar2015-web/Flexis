import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { googleSyncIntervalMs, syncGoogleWorkspace } from "@/shared/api/googleSync";
import { useAuth } from "@/shared/auth/AuthProvider";

type GoogleSyncContextValue = {
  lastSyncedAt: number | null;
  failed: boolean;
  isSyncing: boolean;
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
  const busyRef = useRef(false);
  const lastSyncedAtRef = useRef<number | null>(null);

  const runSync = useCallback(
    async (mode: "auto" | "manual") => {
      if (!auth.user || busyRef.current) {
        return;
      }

      busyRef.current = true;
      setIsSyncing(true);
      try {
        await syncGoogleWorkspace(queryClient, mode);
        const stamped = Date.now();
        lastSyncedAtRef.current = stamped;
        setLastSyncedAt(stamped);
        setFailed(false);
      } catch {
        setFailed(true);
      } finally {
        busyRef.current = false;
        setIsSyncing(false);
      }
    },
    [auth.user, queryClient],
  );

  const refresh = useCallback(() => runSync("manual"), [runSync]);

  useEffect(() => {
    if (!auth.user) {
      lastSyncedAtRef.current = null;
      setLastSyncedAt(null);
      setFailed(false);
      return;
    }

    void runSync("auto");
    const timer = window.setInterval(() => {
      if (document.visibilityState !== "visible") {
        return;
      }

      void runSync("auto");
    }, googleSyncIntervalMs);

    function onVisible() {
      if (document.visibilityState !== "visible") {
        return;
      }

      const last = lastSyncedAtRef.current;
      if (last !== null && Date.now() - last < googleSyncIntervalMs) {
        return;
      }

      void runSync("auto");
    }

    document.addEventListener("visibilitychange", onVisible);
    return () => {
      window.clearInterval(timer);
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [auth.user, runSync]);

  const value = useMemo(
    () => ({ lastSyncedAt, failed, isSyncing, refresh }),
    [failed, isSyncing, lastSyncedAt, refresh],
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
