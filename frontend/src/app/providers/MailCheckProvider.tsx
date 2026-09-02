import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocation } from "react-router-dom";
import { isAbortError } from "@/shared/api/client";
import { isApiError } from "@/shared/api/errors";
import {
  getMailCheckSettings,
  mailCheckAutoIntervalMs,
  mailCheckInboxRootQueryKey,
  mailCheckNeedActionQueryKey,
  mailCheckSettingsQueryKey,
  mailCheckSettingsRevisionQueryKey,
  runMailCheck,
} from "@/shared/api/mailCheck";
import { isMailCheckManualRunActive, bindMailCheckAutoAbort } from "@/features/mailCheck/mailCheckRunCoordination";
import { useAuth } from "@/shared/auth/AuthProvider";
import { appPaths } from "@/shared/config/paths";
import { reportIssue } from "@/shared/notifications/issueStore";
import type { MailCheckRun, MailCheckSettings } from "@/shared/types/mailCheck";

const autoRunTimeoutMs = 120_000;
const autoFollowUpRuns = 5;

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
  const location = useLocation();
  const queryClient = useQueryClient();
  const busyRef = useRef(false);
  const unavailableRef = useRef(false);
  const settingsRevisionRef = useRef(0);
  const [isRunning, setIsRunning] = useState(false);
  const [lastRunAt, setLastRunAt] = useState<number | null>(null);
  const onMailCheck =
    location.pathname === appPaths.mailCheck ||
    location.pathname.startsWith(`${appPaths.mailCheck}/`);

  const settingsQuery = useQuery({
    queryKey: mailCheckSettingsQueryKey,
    queryFn: getMailCheckSettings,
    enabled: Boolean(auth.user && onMailCheck),
  });

  const settings = settingsQuery.data;
  const intervalSeconds = settings?.autoCheckIntervalSeconds ?? 20;
  const isLive = Boolean(
    auth.user &&
      onMailCheck &&
      settings?.autoCheckEnabled &&
      settings.hasApiKey &&
      (settings.mailboxes?.length ?? 0) > 0,
  );

  useEffect(() => {
    if (!auth.user || !onMailCheck) {
      setIsRunning(false);
      return;
    }

    let cancelled = false;
    let timer: ReturnType<typeof setTimeout> | undefined;

    const scheduleNext = (delayMs: number) => {
      if (cancelled) {
        return;
      }

      timer = window.setTimeout(() => {
        void loop();
      }, delayMs);
    };

    const loop = async () => {
      if (cancelled || document.visibilityState !== "visible") {
        if (!cancelled) {
          scheduleNext(5_000);
        }
        return;
      }

      if (busyRef.current || unavailableRef.current || isMailCheckManualRunActive()) {
        const cached = queryClient.getQueryData<MailCheckSettings>(mailCheckSettingsQueryKey);
        scheduleNext(isMailCheckManualRunActive() ? 1_000 : mailCheckAutoIntervalMs(cached?.autoCheckIntervalSeconds ?? 20));
        return;
      }

      busyRef.current = true;
      setIsRunning(true);
      let intervalMs = mailCheckAutoIntervalMs(20);
      let scheduleDelayMs: number | null = intervalMs;
      let fastRetry = false;
      const revisionAtStart =
        queryClient.getQueryData<number>(mailCheckSettingsRevisionQueryKey) ?? 0;
      settingsRevisionRef.current = revisionAtStart;

      try {
        const currentSettings = await getMailCheckSettings();
        if (cancelled) {
          scheduleDelayMs = null;
          return;
        }

        queryClient.setQueryData(mailCheckSettingsQueryKey, currentSettings);
        intervalMs = mailCheckAutoIntervalMs(currentSettings.autoCheckIntervalSeconds);
        scheduleDelayMs = intervalMs;

        if (!currentSettings.autoCheckEnabled) {
          scheduleDelayMs = 30_000;
          return;
        }

        if (!currentSettings.hasApiKey || currentSettings.mailboxes.length === 0) {
          return;
        }

        if (currentSettings.lastError) {
          reportIssue({
            severity: "error",
            source: "mail-check",
            message: currentSettings.lastError,
            method: "POST",
            path: "/api/mail-check/run",
          });
        }

        const runController = new AbortController();
        bindMailCheckAutoAbort(runController);
        const runTimeout = window.setTimeout(() => runController.abort(), autoRunTimeoutMs);
        try {
          if (isMailCheckManualRunActive()) {
            return;
          }

          let result: MailCheckRun = await runMailCheck({
            force: false,
            signal: runController.signal,
          });
          if (!result.busy) {
            let extra = 0;
            while (result.hasMore && extra < autoFollowUpRuns) {
              result = await runMailCheck({
                force: false,
                signal: runController.signal,
              });
              if (result.busy || cancelled) {
                break;
              }

              extra += 1;
            }

            await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
            await queryClient.invalidateQueries({ queryKey: mailCheckInboxRootQueryKey });
            await queryClient.invalidateQueries({ queryKey: mailCheckNeedActionQueryKey });
            setLastRunAt(Date.now());
            if (result.errors > 0) {
              const failed = result.items.filter((item) => item.action === "error");
              reportIssue({
                severity: "error",
                source: "mail-check",
                message: `Mail Check failed on ${result.errors} message(s).`,
                method: "POST",
                path: "/api/mail-check/run",
                detail: failed
                  .map((item) => `${item.from} ${item.subject}: ${item.reason}`.trim())
                  .join("\n"),
              });
            }

            if (result.hasMore) {
              fastRetry = true;
              scheduleDelayMs = 2_000;
            }
          }
        } catch (error) {
          if (isAbortError(error)) {
            fastRetry = true;
            scheduleDelayMs = 2_000;
            return;
          }

          throw error;
        } finally {
          window.clearTimeout(runTimeout);
          bindMailCheckAutoAbort(null);
        }
      } catch (error) {
        if (isApiError(error) && (error.status === 404 || error.status === 401)) {
          unavailableRef.current = true;
          scheduleDelayMs = null;
          return;
        }
        if (!isApiError(error)) {
          reportIssue({
            severity: "error",
            source: "mail-check",
            message: error instanceof Error ? error.message : "Mail Check auto-check failed.",
            method: "POST",
            path: "/api/mail-check/run",
          });
        }
      } finally {
        busyRef.current = false;
        if (!cancelled) {
          setIsRunning(false);
        }
        if (!cancelled && scheduleDelayMs !== null) {
          const latestRevision =
            queryClient.getQueryData<number>(mailCheckSettingsRevisionQueryKey) ?? 0;
          if (latestRevision !== settingsRevisionRef.current) {
            scheduleDelayMs = 0;
          } else if (!fastRetry) {
            scheduleDelayMs = intervalMs;
          }

          scheduleNext(scheduleDelayMs);
        }
      }
    };

    void loop();

    function onVisible() {
      if (document.visibilityState === "visible") {
        void loop();
      }
    }

    document.addEventListener("visibilitychange", onVisible);
    return () => {
      cancelled = true;
      setIsRunning(false);
      if (timer !== undefined) {
        window.clearTimeout(timer);
      }
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [auth.user, onMailCheck, queryClient]);

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
