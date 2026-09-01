import { useCallback, useEffect, useRef, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useLocation } from "react-router-dom";
import {
  getMailCheckSettings,
  mailCheckInboxRootQueryKey,
  mailCheckIntervalMs,
  mailCheckLastRunQueryKey,
  mailCheckSettingsQueryKey,
  runMailCheck,
} from "@/shared/api/mailCheck";
import { isApiError } from "@/shared/api/errors";
import { reportIssue } from "@/shared/notifications/issueStore";
import { useAuth } from "@/shared/auth/AuthProvider";
import { appPaths } from "@/shared/config/paths";
import type { MailCheckRun } from "@/shared/types/mailCheck";

type MailCheckProviderProps = {
  children: ReactNode;
};

export function MailCheckProvider({ children }: MailCheckProviderProps) {
  const auth = useAuth();
  const location = useLocation();
  const queryClient = useQueryClient();
  const busyRef = useRef(false);
  const lastAtRef = useRef<number | null>(null);
  const unavailableRef = useRef(false);
  const onMailCheck = location.pathname === appPaths.mailCheck || location.pathname.startsWith(`${appPaths.mailCheck}/`);

  const runAuto = useCallback(async () => {
    if (!auth.user || !onMailCheck || busyRef.current || unavailableRef.current || document.visibilityState !== "visible") {
      return;
    }

    busyRef.current = true;
    try {
      const settings = await getMailCheckSettings();
      queryClient.setQueryData(mailCheckSettingsQueryKey, settings);
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

      let result: MailCheckRun = await runMailCheck(false);
      if (!result.busy) {
        queryClient.setQueryData(mailCheckLastRunQueryKey, result);
        let extra = 0;
        while (result.hasMore && extra < 2) {
          result = await runMailCheck(false);
          if (result.busy) {
            break;
          }
          const previous = queryClient.getQueryData<MailCheckRun>(mailCheckLastRunQueryKey);
          queryClient.setQueryData(mailCheckLastRunQueryKey, {
            ...result,
            processed: (previous?.processed ?? 0) + result.processed,
            labeled: (previous?.labeled ?? 0) + result.labeled,
            trashed: (previous?.trashed ?? 0) + result.trashed,
            skipped: (previous?.skipped ?? 0) + result.skipped,
            errors: (previous?.errors ?? 0) + result.errors,
            items: [...(previous?.items ?? []), ...result.items],
          });
          extra += 1;
        }

        lastAtRef.current = Date.now();
        await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
        await queryClient.invalidateQueries({ queryKey: mailCheckInboxRootQueryKey });
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
      return;
    } finally {
      busyRef.current = false;
    }
  }, [auth.user, onMailCheck, queryClient]);

  useEffect(() => {
    if (!auth.user || !onMailCheck) {
      return;
    }

    void runAuto();
    const timer = window.setInterval(() => {
      void runAuto();
    }, mailCheckIntervalMs);

    function onVisible() {
      if (document.visibilityState !== "visible") {
        return;
      }

      const last = lastAtRef.current;
      if (last !== null && Date.now() - last < mailCheckIntervalMs) {
        return;
      }

      void runAuto();
    }

    document.addEventListener("visibilitychange", onVisible);
    return () => {
      window.clearInterval(timer);
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [auth.user, onMailCheck, runAuto]);

  return children;
}
