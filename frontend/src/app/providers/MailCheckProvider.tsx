import { useEffect, useRef, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useLocation } from "react-router-dom";
import { isApiError } from "@/shared/api/errors";
import {
  getMailCheckSettings,
  mailCheckAutoIntervalMs,
  mailCheckInboxRootQueryKey,
  mailCheckNeedActionQueryKey,
  mailCheckSettingsQueryKey,
  runMailCheck,
} from "@/shared/api/mailCheck";
import { useAuth } from "@/shared/auth/AuthProvider";
import { appPaths } from "@/shared/config/paths";
import { reportIssue } from "@/shared/notifications/issueStore";
import type { MailCheckRun, MailCheckSettings } from "@/shared/types/mailCheck";

type MailCheckProviderProps = {
  children: ReactNode;
};

export function MailCheckProvider({ children }: MailCheckProviderProps) {
  const auth = useAuth();
  const location = useLocation();
  const queryClient = useQueryClient();
  const busyRef = useRef(false);
  const unavailableRef = useRef(false);
  const onMailCheck = location.pathname === appPaths.mailCheck || location.pathname.startsWith(`${appPaths.mailCheck}/`);

  useEffect(() => {
    if (!auth.user || !onMailCheck) {
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

      if (busyRef.current || unavailableRef.current) {
        const cached = queryClient.getQueryData<MailCheckSettings>(mailCheckSettingsQueryKey);
        scheduleNext(mailCheckAutoIntervalMs(cached?.autoCheckIntervalSeconds ?? 20));
        return;
      }

      busyRef.current = true;
      let intervalMs = mailCheckAutoIntervalMs(20);
      let scheduleDelayMs: number | null = intervalMs;

      try {
        const settings = await getMailCheckSettings();
        if (cancelled) {
          scheduleDelayMs = null;
          return;
        }

        queryClient.setQueryData(mailCheckSettingsQueryKey, settings);
        intervalMs = mailCheckAutoIntervalMs(settings.autoCheckIntervalSeconds);
        scheduleDelayMs = intervalMs;

        if (!settings.autoCheckEnabled) {
          scheduleDelayMs = 30_000;
          return;
        }

        if (!settings.hasApiKey || settings.mailboxes.length === 0) {
          return;
        }

        if (settings.lastError) {
          reportIssue({
            severity: "error",
            source: "mail-check",
            message: settings.lastError,
            method: "POST",
            path: "/api/mail-check/run",
          });
        }

        let result: MailCheckRun = await runMailCheck({ force: false });
        if (!result.busy) {
          let extra = 0;
          while (result.hasMore && extra < 2) {
            result = await runMailCheck({ force: false });
            if (result.busy || cancelled) {
              break;
            }

            extra += 1;
          }

          await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
          await queryClient.invalidateQueries({ queryKey: mailCheckInboxRootQueryKey });
          await queryClient.invalidateQueries({ queryKey: mailCheckNeedActionQueryKey });
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
        }
      } catch (error) {
        if (isApiError(error) && error.status === 404) {
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
        if (!cancelled && scheduleDelayMs !== null) {
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
      if (timer !== undefined) {
        window.clearTimeout(timer);
      }
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [auth.user, onMailCheck, queryClient]);

  return children;
}
