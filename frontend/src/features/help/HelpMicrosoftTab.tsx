import Alert from "@mui/material/Alert";
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
import { Link as RouterLink } from "react-router-dom";
import { ExternalLink, Panel, RedirectUriBlock, outlookRedirectUri } from "@/features/help/helpUi";
import { appPaths } from "@/shared/config/paths";

const scopes = [
  { permission: "Mail.ReadWrite", why: "Read mail, move junk to inbox, trash noise" },
  { permission: "MailboxSettings.ReadWrite", why: "Create master categories (Outlook labels)" },
  { permission: "openid", why: "Sign-in" },
  { permission: "profile", why: "Account identity" },
  { permission: "email", why: "Connected address" },
  { permission: "offline_access", why: "Refresh token" },
] as const;

export function HelpMicrosoftTab() {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Microsoft setup
        </Typography>
        <Typography variant="body2" color="text.secondary">
          One-time Azure work so Mail Check can connect Outlook or Microsoft 365. An admin pastes
          Application ID and client secret on product Settings. Each person still connects their own
          mailbox on Mail Check Settings.
        </Typography>
      </Stack>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Two Microsoft pieces
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary="Flexis Azure app registration"
                secondary="One Application ID and secret for the whole app. An admin pastes them on product Settings. Without this, Connect Outlook stays disabled."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Per-user mailbox"
                secondary="Each Flexis account connects its own Outlook or Microsoft 365 mailbox on Mail Check Settings. Tokens stay on the API, not in the browser."
              />
            </ListItem>
          </List>
          <Alert severity="info">
            Job Application Gmail is separate. Sheets and pipeline still use Google on Job Application
            Settings.
          </Alert>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Redirect URI
          </Typography>
          <Typography variant="body2">
            Paste this exactly on the Azure Web platform. No trailing slash. Do not use 127.0.0.1 for
            the redirect.
          </Typography>
          <RedirectUriBlock uri={outlookRedirectUri} />
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            1. Register an app
          </Typography>
          <Typography variant="body2">
            Open{" "}
            <ExternalLink href="https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade">
              Azure App registrations
            </ExternalLink>
            . New registration. Name flexis-local. Supported account types: Accounts in any
            organizational directory and personal Microsoft accounts. Leave redirect blank for now.
            Register. Copy Application (client) ID from Overview.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            2. Client secret
          </Typography>
          <Typography variant="body2">
            Open Certificates and secrets. New client secret. Description flexis-local. Add. Copy the
            Value now. Azure does not show it again.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            3. Redirect URI
          </Typography>
          <Typography variant="body2">
            Open Authentication. Add a platform. Web. Paste the redirect URI below. Leave implicit
            grant tokens unchecked. Save.
          </Typography>
          <RedirectUriBlock uri={outlookRedirectUri} />
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            4. API permissions
          </Typography>
          <Typography variant="body2">
            Open API permissions. Add a permission. Microsoft Graph. Delegated permissions only.
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">Permission</TableCell>
                  <TableCell align="left">Why</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {scopes.map((scope) => (
                  <TableRow key={scope.permission}>
                    <TableCell align="left">{scope.permission}</TableCell>
                    <TableCell align="left">{scope.why}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <Typography variant="body2" color="text.secondary">
            Grant admin consent if your tenant requires it. Personal Microsoft accounts do not need
            tenant admin consent for these delegated scopes. Do not add application permissions.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            5. Put credentials in Flexis
          </Typography>
          <Typography variant="body2">
            Sign in as an admin. Open{" "}
            <Link component={RouterLink} to={appPaths.settings}>
              Settings
            </Link>
            . Under Microsoft client, paste Application (client) ID and client secret. Save. That is
            one Azure app for the Flexis deployment. Each person still connects their own mailbox on
            Mail Check.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            6. Confirm Flexis is running
          </Typography>
          <Typography variant="body2">
            Saving the Microsoft client in Settings does not need a restart. Health must be Healthy
            at{" "}
            <ExternalLink href="http://localhost:5080/api/health">
              http://localhost:5080/api/health
            </ExternalLink>
            . App at{" "}
            <ExternalLink href="http://127.0.0.1:5173/">http://127.0.0.1:5173/</ExternalLink>.
            Mail Check Settings shows Connect Outlook when the client is saved.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            7. Connect Outlook
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText primary="Open Mail Check, Settings tab. Connect Outlook opens Microsoft sign-in." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Pick the mailbox you want triaged and Accept. Flexis returns to Mail Check. The chip should read Connected." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Paste an OpenAI API key on the same tab before auto-check classifies mail." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary={
                  <Link component={RouterLink} to={appPaths.mailCheck}>
                    Open Mail Check
                  </Link>
                }
              />
            </ListItem>
          </List>
          <Typography variant="body2">
            Flexis creates four Outlook master categories: Interview Scheduled, Waiting for answer,
            Need to Schedule/Availability, and Others.
          </Typography>
        </Stack>
      </Panel>
    </Stack>
  );
}
