import { useCallback, useEffect, useRef, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  getMailCheckSettings,
  mailCheckInboxRootQueryKey,
  mailCheckIntervalMs,
  mailCheckLastRunQueryKey,
  mailCheckSettingsQueryKey,
  runMailCheck,
} from "@/shared/api/mailCheck";
import { useAuth } from "@/shared/auth/AuthProvider";
import type { MailCheckRun } from "@/shared/types/mailCheck";

type MailCheckProviderProps = {
  children: ReactNode;
};

export function MailCheckProvider({ children }: MailCheckProviderProps) {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const busyRef = useRef(false);
  const lastAtRef = useRef<number | null>(null);

  const runAuto = useCallback(async () => {
    if (!auth.user || busyRef.current || document.visibilityState !== "visible") {
      return;
    }

    busyRef.current = true;
    try {
      const settings = await getMailCheckSettings();
      queryClient.setQueryData(mailCheckSettingsQueryKey, settings);
      if (!settings.hasApiKey || !settings.gmailConnected) {
        return;
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
      }
    } catch {
      return;
    } finally {
      busyRef.current = false;
    }
  }, [auth.user, queryClient]);

  useEffect(() => {
    if (!auth.user) {
      lastAtRef.current = null;
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
  }, [auth.user, runAuto]);

  return children;
}
