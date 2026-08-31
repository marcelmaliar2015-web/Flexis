import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Panel } from "@/features/mailCheck/mailCheckLayout";
import {
  disconnectMailCheckMailbox,
  getMailCheckMailbox,
  mailCheckMailboxQueryKey,
  mailCheckSettingsQueryKey,
  startMailCheckGmail,
  startMailCheckOutlook,
} from "@/shared/api/mailCheck";
import { appPaths } from "@/shared/config/paths";

const ProviderCard = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2),
}));

function mailboxNotice(result: string | null): { severity: "success" | "info" | "error"; text: string } | null {
  if (result === "connected") {
    return { severity: "success", text: "Mailbox connected." };
  }
  if (result === "denied") {
    return { severity: "info", text: "Mailbox access was not granted." };
  }
  if (result === "error") {
    return { severity: "error", text: "Mailbox connect did not complete." };
  }
  return null;
}

export function MailCheckMailboxCard() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [notice, setNotice] = useState<ReturnType<typeof mailboxNotice>>(null);

  const mailboxQuery = useQuery({
    queryKey: mailCheckMailboxQueryKey,
    queryFn: getMailCheckMailbox,
  });

  useEffect(() => {
    const result = searchParams.get("mailbox");
    const nextNotice = mailboxNotice(result);
    if (!nextNotice) {
      return;
    }

    setNotice(nextNotice);
    void queryClient.invalidateQueries({ queryKey: mailCheckMailboxQueryKey });
    void queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
    navigate(appPaths.mailCheck, { replace: true });
  }, [navigate, queryClient, searchParams]);

  const connectGmailMutation = useMutation({
    mutationFn: () => startMailCheckGmail(`${window.location.origin}${appPaths.mailCheck}`),
    onSuccess: (result) => {
      window.location.assign(result.authorizationUrl);
    },
  });

  const connectOutlookMutation = useMutation({
    mutationFn: () => startMailCheckOutlook(`${window.location.origin}${appPaths.mailCheck}`),
    onSuccess: (result) => {
      window.location.assign(result.authorizationUrl);
    },
  });

  const disconnectMutation = useMutation({
    mutationFn: disconnectMailCheckMailbox,
    onSuccess: async () => {
      setNotice(null);
      await queryClient.invalidateQueries({ queryKey: mailCheckMailboxQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
    },
  });

  const mailbox = mailboxQuery.data;
  const connected = mailbox?.connected === true;
  const providerLabel =
    mailbox?.provider === "gmail" ? "Gmail" : mailbox?.provider === "outlook" ? "Outlook" : "Mailbox";

  return (
    <Panel>
      <Stack spacing={2}>
        <Stack
          direction="row"
          spacing={2}
          sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
        >
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Mailbox
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Mail Check uses its own mailbox connection. Job Application Gmail stays separate for
              sheets and pipeline.
            </Typography>
          </Stack>
          {connected ? <Chip color="success" label="Connected" /> : <Chip label="Not connected" />}
        </Stack>
        {notice ? <Alert severity={notice.severity}>{notice.text}</Alert> : null}
        {connected && mailbox.email ? (
          <Typography variant="body2">
            {providerLabel} · {mailbox.email}
          </Typography>
        ) : null}
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <ProviderCard sx={{ flex: 1 }}>
            <Stack spacing={1.5}>
              <Stack direction="row" spacing={1} sx={{ alignItems: "center", justifyContent: "space-between" }}>
                <Typography variant="subtitle1">Gmail</Typography>
                {mailbox?.provider === "gmail" && connected ? (
                  <Chip size="small" color="success" label="Active" />
                ) : null}
              </Stack>
              <Typography variant="body2" color="text.secondary">
                Uses Gmail labels and stars. Free except your OpenAI key.
              </Typography>
              {mailbox?.provider === "gmail" && connected ? (
                <Button
                  variant="outlined"
                  disabled={disconnectMutation.isPending}
                  loading={disconnectMutation.isPending}
                  onClick={() => disconnectMutation.mutate()}
                >
                  Disconnect Gmail
                </Button>
              ) : (
                <Button
                  disabled={connectGmailMutation.isPending || (connected && mailbox?.provider !== "gmail")}
                  loading={connectGmailMutation.isPending}
                  onClick={() => connectGmailMutation.mutate()}
                >
                  Connect Gmail
                </Button>
              )}
            </Stack>
          </ProviderCard>
          <ProviderCard sx={{ flex: 1, opacity: mailbox?.outlookAvailable ? 1 : 0.72 }}>
            <Stack spacing={1.5}>
              <Stack direction="row" spacing={1} sx={{ alignItems: "center", justifyContent: "space-between" }}>
                <Typography variant="subtitle1">Outlook</Typography>
                {mailbox?.provider === "outlook" && connected ? (
                  <Chip size="small" color="success" label="Active" />
                ) : null}
              </Stack>
              <Typography variant="body2" color="text.secondary">
                {mailbox?.outlookAvailable
                  ? "Uses Outlook categories and flags. Works with Microsoft 365 and Outlook.com."
                  : "An admin must set Microsoft ClientId, ClientSecret, and RedirectUri in backend appsettings."}
              </Typography>
              {mailbox?.provider === "outlook" && connected ? (
                <Button
                  variant="outlined"
                  disabled={disconnectMutation.isPending}
                  loading={disconnectMutation.isPending}
                  onClick={() => disconnectMutation.mutate()}
                >
                  Disconnect Outlook
                </Button>
              ) : (
                <Button
                  variant="outlined"
                  disabled={
                    !mailbox?.outlookAvailable
                    || connectOutlookMutation.isPending
                    || (connected && mailbox?.provider !== "outlook")
                  }
                  loading={connectOutlookMutation.isPending}
                  onClick={() => connectOutlookMutation.mutate()}
                >
                  Connect Outlook
                </Button>
              )}
            </Stack>
          </ProviderCard>
        </Stack>
      </Stack>
    </Panel>
  );
}
