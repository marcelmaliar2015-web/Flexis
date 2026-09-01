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
import { providerLabel } from "@/features/mailCheck/mailCheckUi";
import {
  disconnectMailCheckMailbox,
  getMailCheckMailbox,
  mailCheckMailboxQueryKey,
  mailCheckSettingsQueryKey,
  startMailCheckGmail,
  startMailCheckOutlook,
} from "@/shared/api/mailCheck";
import { appPaths } from "@/shared/config/paths";
import type { MailCheckMailboxItem } from "@/shared/types/mailCheck";

const ProviderCard = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2),
}));

const MailboxRow = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(1.5, 2),
  display: "flex",
  alignItems: "center",
  justifyContent: "space-between",
  gap: theme.spacing(2),
  flexWrap: "wrap",
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

function formatConnectedAt(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function MailCheckMailboxCard() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [notice, setNotice] = useState<ReturnType<typeof mailboxNotice>>(null);
  const [disconnectingId, setDisconnectingId] = useState<string | null>(null);

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
    mutationFn: (id: string) => disconnectMailCheckMailbox(id),
    onMutate: (id) => {
      setDisconnectingId(id);
    },
    onSuccess: async () => {
      setNotice(null);
      await queryClient.invalidateQueries({ queryKey: mailCheckMailboxQueryKey });
      await queryClient.invalidateQueries({ queryKey: mailCheckSettingsQueryKey });
    },
    onSettled: () => {
      setDisconnectingId(null);
    },
  });

  const mailboxes = mailboxQuery.data?.mailboxes ?? [];
  const outlookAvailable = mailboxQuery.data?.outlookAvailable === true;

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
              Mailboxes
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Connect any number of Gmail and Outlook accounts. Check all and Inbox use every
              connected mailbox. Job Application Gmail stays separate for sheets and pipeline.
            </Typography>
          </Stack>
          {mailboxes.length > 0 ? (
            <Chip color="success" label={`${mailboxes.length} connected`} />
          ) : (
            <Chip label="None connected" />
          )}
        </Stack>
        {notice ? <Alert severity={notice.severity}>{notice.text}</Alert> : null}
        {mailboxes.length > 0 ? (
          <Stack spacing={1}>
            {mailboxes.map((mailbox: MailCheckMailboxItem) => (
              <MailboxRow key={mailbox.id}>
                <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                  <Typography variant="body2">
                    {providerLabel(mailbox.provider)} · {mailbox.email}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Connected {formatConnectedAt(mailbox.connectedAt)}
                  </Typography>
                </Stack>
                <Button
                  variant="outlined"
                  disabled={disconnectMutation.isPending}
                  loading={disconnectingId === mailbox.id}
                  onClick={() => disconnectMutation.mutate(mailbox.id)}
                >
                  Disconnect
                </Button>
              </MailboxRow>
            ))}
          </Stack>
        ) : null}
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <ProviderCard sx={{ flex: 1 }}>
            <Stack spacing={1.5}>
              <Typography variant="subtitle1">Gmail</Typography>
              <Typography variant="body2" color="text.secondary">
                Uses Gmail labels and stars. Add another Gmail account any time.
              </Typography>
              <Button
                disabled={connectGmailMutation.isPending}
                loading={connectGmailMutation.isPending}
                onClick={() => connectGmailMutation.mutate()}
              >
                Add Gmail
              </Button>
            </Stack>
          </ProviderCard>
          <ProviderCard sx={{ flex: 1, opacity: outlookAvailable ? 1 : 0.72 }}>
            <Stack spacing={1.5}>
              <Typography variant="subtitle1">Outlook</Typography>
              <Typography variant="body2" color="text.secondary">
                {outlookAvailable
                  ? "Uses Outlook categories and flags. Add Microsoft 365 or Outlook.com accounts."
                  : "An admin must save the Microsoft client on Settings before Add Outlook is available."}
              </Typography>
              <Button
                variant="outlined"
                disabled={!outlookAvailable || connectOutlookMutation.isPending}
                loading={connectOutlookMutation.isPending}
                onClick={() => connectOutlookMutation.mutate()}
              >
                Add Outlook
              </Button>
            </Stack>
          </ProviderCard>
        </Stack>
      </Stack>
    </Panel>
  );
}
