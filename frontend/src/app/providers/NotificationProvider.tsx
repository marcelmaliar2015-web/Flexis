import {
  useEffect,
  useState,
  type ReactNode,
} from "react";
import Alert from "@mui/material/Alert";
import Snackbar from "@mui/material/Snackbar";
import { ApiError } from "@/shared/api/client";
import {
  dismissIssueToast,
  getIssueToast,
  reportIssue,
  subscribeIssues,
} from "@/shared/notifications/issueStore";

type NotificationProviderProps = {
  children: ReactNode;
};

export function NotificationProvider({ children }: NotificationProviderProps) {
  const [toast, setToast] = useState(getIssueToast());

  useEffect(() => {
    return subscribeIssues(() => {
      setToast(getIssueToast());
    });
  }, []);

  useEffect(() => {
    function onWindowError(event: ErrorEvent) {
      const message = event.error instanceof Error ? event.error.message : event.message;
      if (!message || message.includes("ResizeObserver")) {
        return;
      }
      reportIssue({
        severity: "error",
        source: "window",
        message,
        detail: event.error instanceof Error ? event.error.stack : undefined,
      });
    }

    function onUnhandled(event: PromiseRejectionEvent) {
      const reason = event.reason;
      if (reason instanceof ApiError) {
        return;
      }
      const message = reason instanceof Error ? reason.message : String(reason ?? "Unhandled promise rejection.");
      if (message.includes("ResizeObserver") || message.startsWith("API is not running.")) {
        return;
      }
      reportIssue({
        severity: "error",
        source: "window",
        message,
        detail: reason instanceof Error ? reason.stack : undefined,
      });
    }

    window.addEventListener("error", onWindowError);
    window.addEventListener("unhandledrejection", onUnhandled);
    return () => {
      window.removeEventListener("error", onWindowError);
      window.removeEventListener("unhandledrejection", onUnhandled);
    };
  }, []);

  return (
    <>
      {children}
      <Snackbar
        open={toast !== null}
        autoHideDuration={8000}
        onClose={dismissIssueToast}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        {toast ? (
          <Alert
            severity={toast.severity}
            variant="filled"
            onClose={dismissIssueToast}
          >
            {toast.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </>
  );
}
