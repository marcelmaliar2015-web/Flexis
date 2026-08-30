import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Link from "@mui/material/Link";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { appPaths } from "@/shared/config/paths";

const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3),
}));

const redirectUri = "http://localhost:5080/api/google/connections/callback";

const apiEnableLinks = [
  {
    label: "Gmail API",
    href: "https://console.cloud.google.com/apis/library/gmail.googleapis.com",
  },
  {
    label: "Google Sheets API",
    href: "https://console.cloud.google.com/apis/library/sheets.googleapis.com",
  },
  {
    label: "Google Drive API",
    href: "https://console.cloud.google.com/apis/library/drive.googleapis.com",
  },
] as const;

const scopes = [
  {
    api: "Gmail API",
    label: "Read, compose, and send emails from your Gmail account",
    uri: "https://www.googleapis.com/auth/gmail.modify",
    kind: "Restricted",
  },
  {
    api: "Google Sheets API",
    label: "See, edit, create, and delete all your Google Sheets spreadsheets",
    uri: "https://www.googleapis.com/auth/spreadsheets",
    kind: "Sensitive",
  },
  {
    api: "Google Drive API",
    label: "See, edit, create, and delete only the specific Google Drive files you use with this app",
    uri: "https://www.googleapis.com/auth/drive.file",
    kind: "Sensitive",
  },
  {
    api: "Google OAuth2 API",
    label: "See your primary Google Account email address",
    uri: "https://www.googleapis.com/auth/userinfo.email",
    kind: "Non-sensitive",
  },
  {
    api: "OpenID",
    label: "openid",
    uri: "openid",
    kind: "Non-sensitive",
  },
] as const;

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

function RedirectUriBlock() {
  const [copied, setCopied] = useState(false);

  return (
    <Stack spacing={1}>
      <Typography variant="body2" component="code">
        {redirectUri}
      </Typography>
      <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
        <Button
          variant="outlined"
          onClick={() => {
            void copyText(redirectUri).then(() => setCopied(true));
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

function ExternalLink({ href, children }: { href: string; children: string }) {
  return (
    <Link href={href} target="_blank" rel="noopener noreferrer">
      {children}
    </Link>
  );
}

export function HelpPage() {
  return (
    <Box sx={{ py: { xs: 4, md: 6 } }}>
      <Container maxWidth="md">
        <Stack spacing={3}>
          <Stack spacing={1}>
            <Typography variant="overline" color="secondary">
              Help
            </Typography>
            <Typography variant="h4" component="h1">
              Gmail connect
            </Typography>
            <AccentRule />
            <Typography variant="body2" color="text.secondary">
              One-time Google Cloud setup so Job Application can connect Gmail, Sheets, and Drive.
              Billing is not required. Use the Google account you will connect in Flexis.
            </Typography>
          </Stack>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                Where Flexis stores files
              </Typography>
              <Typography variant="body2">
                After Gmail is connected, Flexis creates this folder tree in that Google Drive and
                puts every profile and source spreadsheet in it. Location tabs stay inside each
                source workbook.
              </Typography>
              <List disablePadding>
                <ListItem disableGutters>
                  <ListItemText primary="Flexis" secondary="Workspace root for this app" />
                </ListItem>
              </List>
              <Box sx={{ pl: 3 }}>
                <List disablePadding>
                  <ListItem disableGutters>
                    <ListItemText
                      primary="Job Application"
                      secondary="Spreadsheets Flexis creates for this feature"
                    />
                  </ListItem>
                </List>
                <Box sx={{ pl: 3 }}>
                  <List disablePadding>
                    <ListItem disableGutters>
                      <ListItemText primary="Profiles" secondary="One Google Sheet per profile" />
                    </ListItem>
                    <ListItem disableGutters>
                      <ListItemText primary="Sources" secondary="One Google Sheet per source" />
                    </ListItem>
                  </List>
                </Box>
              </Box>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                Redirect URI
              </Typography>
              <Typography variant="body2">
                Paste this exactly on the web client. No trailing slash.
              </Typography>
              <RedirectUriBlock />
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                1. Create a project
              </Typography>
              <Typography variant="body2">
                Open{" "}
                <ExternalLink href="https://console.cloud.google.com/projectcreate">
                  https://console.cloud.google.com/projectcreate
                </ExternalLink>
                . Name it flexis-local. Create. Skip billing. Confirm the top bar shows this
                project.
              </Typography>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                2. Enable APIs
              </Typography>
              <Typography variant="body2">Open each URL, then click Enable.</Typography>
              <List disablePadding>
                {apiEnableLinks.map((item) => (
                  <ListItem key={item.href} disableGutters>
                    <ListItemText
                      primary={item.label}
                      secondary={<ExternalLink href={item.href}>{item.href}</ExternalLink>}
                    />
                  </ListItem>
                ))}
              </List>
              <Typography variant="body2" color="text.secondary">
                If the page says Manage, it is already on.
              </Typography>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                3. Branding
              </Typography>
              <Typography variant="body2">
                Open{" "}
                <ExternalLink href="https://console.cloud.google.com/auth/overview">
                  https://console.cloud.google.com/auth/overview
                </ExternalLink>
                . If you see Get started, click it. App name Flexis. User support email: your
                Gmail. Audience: External. Developer contact: your Gmail. Finish.
              </Typography>
              <Typography variant="body2">
                If branding already exists, open{" "}
                <ExternalLink href="https://console.cloud.google.com/auth/branding">
                  https://console.cloud.google.com/auth/branding
                </ExternalLink>{" "}
                and set the same app name.
              </Typography>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                4. Test user
              </Typography>
              <Typography variant="body2">
                Open{" "}
                <ExternalLink href="https://console.cloud.google.com/auth/audience">
                  https://console.cloud.google.com/auth/audience
                </ExternalLink>
                . Publishing status must stay Testing. Under Test users, add the Gmail you will
                use on Connect Gmail. Save.
              </Typography>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                5. Scopes
              </Typography>
              <Typography variant="body2">
                Open{" "}
                <ExternalLink href="https://console.cloud.google.com/auth/scopes">
                  https://console.cloud.google.com/auth/scopes
                </ExternalLink>
                . The picker does not list the raw URLs as options. Finish step 2 first. Only
                enabled APIs show scopes.
              </Typography>
              <Typography variant="body2">Click Add or remove scopes. Then either:</Typography>
              <List disablePadding>
                <ListItem disableGutters>
                  <ListItemText primary="Search the table for Gmail API, Google Sheets API, or Google Drive API, and check the Console name below." />
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Or scroll to Manually add scopes, paste each URI, click Add to table, then Update." />
                </ListItem>
              </List>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Console name</TableCell>
                      <TableCell>URI to paste</TableCell>
                      <TableCell>Kind</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {scopes.map((scope) => (
                      <TableRow key={scope.uri}>
                        <TableCell>
                          {scope.label}
                          <Typography variant="caption" color="text.secondary" component="div">
                            {scope.api}
                          </Typography>
                        </TableCell>
                        <TableCell>{scope.uri}</TableCell>
                        <TableCell>{scope.kind}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
              <Typography variant="body2" color="text.secondary">
                Gmail modify is restricted. That is expected. Stay on Testing. Do not pick See
                and download all your Gmail, mail.google.com, or See, edit, create, and delete
                all of your Google Drive files.
              </Typography>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                6. Web client
              </Typography>
              <Typography variant="body2">
                Open{" "}
                <ExternalLink href="https://console.cloud.google.com/auth/clients">
                  https://console.cloud.google.com/auth/clients
                </ExternalLink>
                . Create client. Application type: Web application. Name: flexis-local-web.
              </Typography>
              <Typography variant="body2">Authorized JavaScript origins:</Typography>
              <List disablePadding>
                <ListItem disableGutters>
                  <ListItemText primary="http://localhost:5173" />
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="http://127.0.0.1:5173" />
                </ListItem>
              </List>
              <Typography variant="body2">Authorized redirect URI (exact):</Typography>
              <RedirectUriBlock />
              <Typography variant="body2">
                Create. Copy Client ID and Client secret. Do not use Desktop, Android, or iOS. Do
                not use 127.0.0.1:5080 for the redirect.
              </Typography>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                7. Put credentials in Flexis
              </Typography>
              <Typography variant="body2">
                Sign in as an admin. Open Settings. Under Google Cloud client, paste Client ID and
                Client secret. Save. That is one client for the Flexis app. Each person still
                connects their own Gmail on Job Application. Do not put the secret in a committed
                project file.
              </Typography>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                8. Confirm Flexis is running
              </Typography>
              <Typography variant="body2">
                Saving the Google Cloud client in Settings does not need a restart. Health must be
                Healthy at{" "}
                <ExternalLink href="http://localhost:5080/api/health">
                  http://localhost:5080/api/health
                </ExternalLink>
                . App at{" "}
                <ExternalLink href="http://127.0.0.1:5173/">http://127.0.0.1:5173/</ExternalLink>.
              </Typography>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                9. Connect
              </Typography>
              <List disablePadding>
                <ListItem disableGutters>
                  <ListItemText primary="Sign in, open Job Application, Settings tab, Connect Gmail." />
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText
                    primary={
                      <Link component={RouterLink} to={appPaths.jobApplication}>
                        Open Job Application
                      </Link>
                    }
                  />
                </ListItem>
              </List>
            </Stack>
          </Panel>

          <Panel>
            <Stack spacing={1.5}>
              <Typography variant="h6" component="h2">
                If Connect fails
              </Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>What you see</TableCell>
                      <TableCell>Fix</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    <TableRow>
                      <TableCell>gmail.modify or spreadsheets not in the list</TableCell>
                      <TableCell>
                        Enable the APIs in step 2, then Manually add scopes and paste the URIs
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Connect Gmail stays disabled</TableCell>
                      <TableCell>An admin must save the Google Cloud client in Settings</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>redirect_uri_mismatch</TableCell>
                      <TableCell>
                        Paste http://localhost:5080/api/google/connections/callback on the web
                        client. Not 127.0.0.1 and no trailing slash.
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Access blocked</TableCell>
                      <TableCell>Add that Gmail under Test users; stay on Testing</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>403 API not enabled</TableCell>
                      <TableCell>Repeat step 2 on the same project as the client</TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </Stack>
          </Panel>
        </Stack>
      </Container>
    </Box>
  );
}
