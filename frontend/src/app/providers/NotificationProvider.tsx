import {
  useEffect,
  useState,
  type ReactNode,
} from "react";
import Alert from "@mui/material/Alert";
import Snackbar from "@mui/material/Snackbar";
import { isApiError } from "@/shared/api/errors";
import {
  dismissIssueToast,
  getIssueToast,
  reportIssue,
  restoreIssueToast,
  subscribeIssues,
} from "@/shared/notifications/issueStore";

type NotificationProviderProps = {
  children: ReactNode;
};

export function NotificationProvider({ children }: NotificationProviderProps) {
  const [toast, setToast] = useState(getIssueToast());

  useEffect(() => {
    const unsubscribe = subscribeIssues(() => {
      setToast(getIssueToast());
    });
    restoreIssueToast();
    setToast(getIssueToast());
    return unsubscribe;
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
      if (isApiError(reason)) {
        return;
      }
      const message = reason instanceof Error ? reason.message : String(reason ?? "Unhandled promise rejection.");
      if (message.includes("ResizeObserver") || message.startsWith("Could not reach the API.")) {
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
        key={toast?.id}
        open={toast !== null}
        autoHideDuration={toast?.severity === "error" ? null : 8000}
        onClose={(_event, reason) => {
          if (reason === "clickaway") {
            return;
          }
          dismissIssueToast();
        }}
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
