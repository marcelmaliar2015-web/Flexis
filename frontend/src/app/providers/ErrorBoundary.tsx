import { Component, type ErrorInfo, type ReactNode } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import { reportIssue } from "@/shared/notifications/issueStore";

type ErrorBoundaryProps = {
  children: ReactNode;
};

type ErrorBoundaryState = {
  message: string | null;
};

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { message: null };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { message: error.message };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    reportIssue({
      severity: "error",
      source: "ui",
      message: error.message,
      detail: [error.stack, info.componentStack].filter(Boolean).join("\n"),
    });
  }

  render() {
    if (this.state.message) {
      return (
        <Box sx={{ p: 3 }}>
          <Alert severity="error">{this.state.message}</Alert>
        </Box>
      );
    }

    return this.props.children;
  }
}
