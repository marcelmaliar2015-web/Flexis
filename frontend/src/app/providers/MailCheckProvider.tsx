import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useQuery } from "@tanstack/react-query";
import {
  getMailCheckRunProgress,
  getMailCheckSettings,
  mailCheckAutoIntervalMs,
  mailCheckRunProgressQueryKey,
  mailCheckSettingsQueryKey,
} from "@/shared/api/mailCheck";
import { useAuth } from "@/shared/auth/AuthProvider";

type MailCheckAutoContextValue = {
  isLive: boolean;
  isRunning: boolean;
  intervalSeconds: number;
  lastRunAt: number | null;
};

const MailCheckAutoContext = createContext<MailCheckAutoContextValue | null>(null);

type MailCheckProviderProps = {
  children: ReactNode;
};

export function MailCheckProvider({ children }: MailCheckProviderProps) {
  const auth = useAuth();
  const [lastRunAt, setLastRunAt] = useState<number | null>(null);

  const settingsQuery = useQuery({
    queryKey: mailCheckSettingsQueryKey,
    queryFn: getMailCheckSettings,
    enabled: Boolean(auth.user),
    refetchInterval: auth.user ? mailCheckAutoIntervalMs(20) : false,
  });

  const settings = settingsQuery.data;
  const intervalSeconds = settings?.autoCheckIntervalSeconds ?? 20;
  const isLive = Boolean(
    auth.user &&
      settings?.autoCheckEnabled &&
      settings.hasApiKey &&
      (settings.mailboxes?.length ?? 0) > 0,
  );

  const progressQuery = useQuery({
    queryKey: mailCheckRunProgressQueryKey,
    queryFn: getMailCheckRunProgress,
    enabled: isLive,
    refetchInterval: isLive ? 2_000 : false,
  });

  const isRunning = Boolean(
    progressQuery.data?.active && progressQuery.data.activeRunKind === "auto",
  );

  useEffect(() => {
    if (!settings?.lastRunAt) {
      return;
    }

    const parsed = Date.parse(settings.lastRunAt);
    if (!Number.isNaN(parsed)) {
      setLastRunAt((current) => (current === null ? parsed : Math.max(current, parsed)));
    }
  }, [settings?.lastRunAt]);

  const value = useMemo(
    () => ({
      isLive,
      isRunning,
      intervalSeconds,
      lastRunAt,
    }),
    [intervalSeconds, isLive, isRunning, lastRunAt],
  );

  return <MailCheckAutoContext.Provider value={value}>{children}</MailCheckAutoContext.Provider>;
}

export function useMailCheckAuto(): MailCheckAutoContextValue {
  const value = useContext(MailCheckAutoContext);
  if (!value) {
    throw new Error("useMailCheckAuto must be used within MailCheckProvider.");
  }

  return value;
}
