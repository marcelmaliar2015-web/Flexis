import CssBaseline from "@mui/material/CssBaseline";
import { ThemeProvider } from "@mui/material/styles";
import { QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { queryClient } from "@/app/providers/queryClient";
import { theme } from "@/app/providers/theme";
import { GoogleSyncProvider } from "@/app/providers/GoogleSyncProvider";
import { MailCheckProvider } from "@/app/providers/MailCheckProvider";
import { AuthProvider } from "@/shared/auth/AuthProvider";

type AppProvidersProps = {
  children: ReactNode;
};

export function AppProviders({ children }: AppProvidersProps) {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <AuthProvider>
          <GoogleSyncProvider>
            <MailCheckProvider>{children}</MailCheckProvider>
          </GoogleSyncProvider>
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
