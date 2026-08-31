import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useState, type ReactNode } from "react";

export const googleRedirectUri = "http://localhost:5080/api/google/connections/callback";

export const outlookRedirectUri =
  "http://localhost:5080/api/mail-check/mailbox/outlook/callback";

export const redirectUri = googleRedirectUri;

export const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3),
}));

export function copyText(value: string): Promise<void> {
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

export function ExternalLink({ href, children }: { href: string; children: ReactNode }) {
  return (
    <Link href={href} target="_blank" rel="noopener noreferrer">
      {children}
    </Link>
  );
}

export function RedirectUriBlock({ uri = googleRedirectUri }: { uri?: string }) {
  const [copied, setCopied] = useState(false);

  return (
    <Stack spacing={1}>
      <Typography variant="body2" component="code">
        {uri}
      </Typography>
      <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
        <Button
          variant="outlined"
          onClick={() => {
            void copyText(uri).then(() => setCopied(true));
          }}
        >
          Copy URL
        </Button>
        {copied ? (
          <Typography variant="body2" color="text.secondary">
            Copied
          </Typography>
        ) : null}
      </Stack>
    </Stack>
  );
}
