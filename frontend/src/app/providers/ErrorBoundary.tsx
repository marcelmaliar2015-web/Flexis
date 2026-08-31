import { Component, type ErrorInfo, type ReactNode } from "react";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import { reportIssue } from "@/shared/notifications/issueStore";

type ErrorBoundaryProps = {
  children: ReactNode;
};

type ErrorBoundaryState = {
  broken: boolean;
};

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { broken: false };

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { broken: true };
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
    if (this.state.broken) {
      return (
        <Box sx={{ p: 3 }}>
          <Typography variant="body2" color="text.secondary">
            Something broke. Open Issues in the header for details.
          </Typography>
        </Box>
      );
    }

    return this.props.children;
  }
}
