import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Link from "@mui/material/Link";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Link as RouterLink, useNavigate, useSearchParams } from "react-router-dom";
import {
  disconnectGoogleConnection,
  getGoogleConnection,
  googleConnectionQueryKey,
  startGoogleConnection,
} from "@/shared/api/google";
import { useAuth } from "@/shared/auth/AuthProvider";
import { appPaths } from "@/shared/config/paths";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3.5),
}));

function copyText(value: string): Promise<void> {
  if (navigator.clipboard?.writeText) {
    return navigator.clipboard.writeText(value);
  }

  return new Promise((resolve, reject) => {
    const field = document.createElement("textarea");
    field.value = value;
    field.setAttribute("readonly", "");
    document.body.appendChild(field);
    field.select();
    const copied = document.execCommand("copy");
    field.remove();
    if (copied) {
      resolve();
      return;
    }

    reject(new Error("Copy failed."));
  });
}

function googleNotice(result: string | null): { severity: "success" | "info" | "error"; text: string } | null {
  if (result === "connected") {
    return { severity: "success", text: "Gmail is connected." };
  }
  if (result === "denied") {
    return { severity: "info", text: "Google access was not granted." };
  }
  if (result === "error") {
    return { severity: "error", text: "Google connect did not complete." };
  }
  return null;
}

export function JobApplicationGmailCard() {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [notice, setNotice] = useState<ReturnType<typeof googleNotice>>(null);

  const connectionQuery = useQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });

  useEffect(() => {
    const result = searchParams.get("google");
    const nextNotice = googleNotice(result);
    if (!nextNotice) {
      return;
    }

    setNotice(nextNotice);
    void queryClient.invalidateQueries({ queryKey: googleConnectionQueryKey });
    navigate(appPaths.jobApplication, { replace: true });
  }, [navigate, queryClient, searchParams]);

  const connectMutation = useMutation({
    mutationFn: () => startGoogleConnection(`${window.location.origin}${appPaths.jobApplication}`),
    onSuccess: (result) => {
      window.location.assign(result.authorizationUrl);
    },
  });

  const copyUrlMutation = useMutation({
    mutationFn: async () => {
      const result = await startGoogleConnection(`${window.location.origin}${appPaths.jobApplication}`);
      await copyText(result.authorizationUrl);
    },
    onSuccess: () => {
      setNotice({ severity: "success", text: "Connect URL copied. Paste it in another browser." });
    },
  });

  const disconnectMutation = useMutation({
    mutationFn: disconnectGoogleConnection,
    onSuccess: async () => {
      setNotice(null);
      await queryClient.invalidateQueries({ queryKey: googleConnectionQueryKey });
    },
  });

  const status = connectionQuery.data;
  const actionError =
    connectMutation.error instanceof Error
      ? connectMutation.error.message
      : copyUrlMutation.error instanceof Error
        ? copyUrlMutation.error.message
        : disconnectMutation.error instanceof Error
          ? disconnectMutation.error.message
          : connectionQuery.error instanceof Error
            ? connectionQuery.error.message
            : null;

  return (
    <Stack spacing={2}>
      {notice ? <Alert severity={notice.severity}>{notice.text}</Alert> : null}
      {actionError ? <Alert severity="error">{actionError}</Alert> : null}
      <Panel>
        {connectionQuery.isPending && !status ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
            <CircularProgress />
          </Box>
        ) : (
          <Stack spacing={2.5}>
            <Stack
              direction="row"
              spacing={2}
              sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
            >
              <Stack spacing={1}>
                <Typography variant="h6" component="h2">
                  Gmail
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  The same Google consent covers Sheets and Drive. Flexis keeps its files under
                  Flexis / Job Application / Profiles and Sources. Drive access stays limited to
                  files this app creates or you open with Flexis.
                </Typography>
              </Stack>
              {status?.connected ? (
                <Chip color="success" label="Connected" />
              ) : (
                <Chip label="Not connected" />
              )}
            </Stack>
            {status && !status.configured ? (
              <Alert severity="info">
                An admin must save the Google Cloud client in Settings first. That is one client
                for Flexis, not each person's Gmail.{" "}
                {auth.user?.role === "Admin" ? (
                  <Link component={RouterLink} to={appPaths.settings}>
                    Open Settings
                  </Link>
                ) : (
                  <Link component={RouterLink} to={appPaths.help}>
                    Open Help
                  </Link>
                )}
              </Alert>
            ) : null}
            {status?.connected && status.googleEmail ? (
              <Typography variant="body2">{status.googleEmail}</Typography>
            ) : null}
            <List disablePadding>
              {(status?.capabilities ?? []).map((capability) => (
                <ListItem key={capability} disableGutters>
                  <ListItemText primary={capability} />
                </ListItem>
              ))}
            </List>
            <Stack direction="row" spacing={1}>
              <Button
                disabled={!status?.configured || status.connected || connectMutation.isPending}
                loading={connectMutation.isPending}
                onClick={() => connectMutation.mutate()}
              >
                Connect Gmail
              </Button>
              <Button
                variant="outlined"
                disabled={!status?.configured || status.connected || copyUrlMutation.isPending}
                loading={copyUrlMutation.isPending}
                onClick={() => copyUrlMutation.mutate()}
              >
                Copy URL
              </Button>
              {status?.connected ? (
                <Button
                  variant="outlined"
                  disabled={disconnectMutation.isPending}
                  loading={disconnectMutation.isPending}
                  onClick={() => disconnectMutation.mutate()}
                >
                  Disconnect
                </Button>
              ) : null}
            </Stack>
          </Stack>
        )}
      </Panel>
    </Stack>
  );
}
