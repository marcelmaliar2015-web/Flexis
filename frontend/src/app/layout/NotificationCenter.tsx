import Alert from "@mui/material/Alert";
import Badge from "@mui/material/Badge";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Drawer from "@mui/material/Drawer";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useEffect, useState } from "react";
import {
  clearIssueNotices,
  formatIssueLog,
  getIssueNotices,
  subscribeIssues,
} from "@/shared/notifications/issueStore";
import type { IssueNotice } from "@/shared/types/issue";

const Trigger = styled(Button)(({ theme }) => ({
  minHeight: 38,
  minWidth: 0,
  paddingLeft: theme.spacing(1.5),
  paddingRight: theme.spacing(1.5),
  borderRadius: 999,
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  color: theme.palette.text.primary,
  boxShadow: "none",
  "&:hover": {
    backgroundColor: theme.palette.action.hover,
    borderColor: theme.palette.primary.light,
    boxShadow: "none",
  },
}));

const DetailBlock = styled("pre")(({ theme }) => ({
  margin: 0,
  whiteSpace: "pre-wrap",
  fontFamily: "inherit",
  fontSize: theme.typography.caption.fontSize,
}));

const DrawerBody = styled(Box)(({ theme }) => ({
  width: 420,
  maxWidth: "100vw",
  height: "100%",
  display: "flex",
  flexDirection: "column",
  backgroundColor: theme.palette.background.default,
}));

function requestLine(notice: IssueNotice): string {
  const parts: string[] = [];
  if (notice.method || notice.path) {
    parts.push([notice.method, notice.path].filter(Boolean).join(" "));
  }
  if (notice.status !== undefined) {
    parts.push(`HTTP ${notice.status}`);
  }
  parts.push(notice.source);
  return parts.join(" · ");
}

export function NotificationCenter() {
  const [notices, setNotices] = useState(getIssueNotices);
  const [open, setOpen] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    return subscribeIssues(() => {
      setNotices(getIssueNotices());
    });
  }, []);

  const errorCount = notices.filter((item) => item.severity === "error").length;

  return (
    <>
      <Badge
        color={errorCount > 0 ? "error" : "warning"}
        badgeContent={notices.length}
        max={99}
        overlap="circular"
      >
        <Trigger
          variant="outlined"
          aria-label="Open issues"
          onClick={() => {
            setCopied(false);
            setOpen(true);
          }}
        >
          Issues
        </Trigger>
      </Badge>
      <Drawer anchor="right" open={open} onClose={() => setOpen(false)}>
        <DrawerBody>
          <Stack spacing={2} sx={{ p: 2.5, borderBottom: 1, borderColor: "divider" }}>
            <Stack spacing={0.5}>
              <Typography variant="overline" color="secondary">
                Issues
              </Typography>
              <Typography variant="h6" component="h2">
                Errors and warnings
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Newest first. Copy this list when something breaks so it can be fixed from the
                exact request, status, and message. The same lines are written to
                .flexis/issue-log.jsonl when the API is running.
              </Typography>
            </Stack>
            <Stack direction="row" spacing={1}>
              <Button
                variant="outlined"
                disabled={notices.length === 0}
                onClick={() => {
                  void navigator.clipboard.writeText(formatIssueLog(notices)).then(() => {
                    setCopied(true);
                  });
                }}
              >
                Copy all
              </Button>
              <Button
                variant="outlined"
                disabled={notices.length === 0}
                onClick={() => {
                  setCopied(false);
                  clearIssueNotices();
                }}
              >
                Clear
              </Button>
            </Stack>
            {copied ? (
              <Typography variant="body2" color="text.secondary">
                Copied
              </Typography>
            ) : null}
          </Stack>
          <Box sx={{ flex: 1, minHeight: 0, overflow: "auto", p: 2.5 }}>
            {notices.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No issues yet.
              </Typography>
            ) : (
              <Stack spacing={1.5}>
                {notices.map((notice) => (
                  <Alert key={notice.id} severity={notice.severity} variant="outlined">
                    <Stack spacing={0.75}>
                      <Typography variant="body2">{notice.message}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {new Date(notice.occurredAt).toLocaleString()} · {requestLine(notice)}
                      </Typography>
                      {notice.detail ? <DetailBlock>{notice.detail}</DetailBlock> : null}
                    </Stack>
                  </Alert>
                ))}
              </Stack>
            )}
          </Box>
        </DrawerBody>
      </Drawer>
    </>
  );
}
