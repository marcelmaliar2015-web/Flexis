import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
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
import { ExternalLink, Panel, RedirectUriBlock } from "@/features/help/helpUi";
import { appPaths } from "@/shared/config/paths";

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

export function HelpGoogleTab() {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Google setup
        </Typography>
        <Typography variant="body2" color="text.secondary">
          One-time Google Cloud work so Job Application can connect Gmail, Sheets, and Drive. Billing
          is not required. Use the Google account you will connect in Flexis. Stay on Testing. Do not
          ask Google for full Drive or full Gmail.
        </Typography>
      </Stack>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Two Google pieces
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary="Flexis Google Cloud web client"
                secondary="One Client ID and secret for the whole app. An admin pastes them on product Settings. Without this, Connect Gmail stays disabled."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Per-user Gmail"
                secondary="Each Flexis account connects its own Google account on Job Application Settings. Tokens stay on the API, not in the browser."
              />
            </ListItem>
          </List>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Where Flexis stores files
          </Typography>
          <Typography variant="body2">
            After Gmail is connected, Flexis creates this folder tree in that Google Drive and puts
            every profile and source spreadsheet in it. Location tabs stay inside each source
            workbook. Disconnect does not delete the tree or the sheets.
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
            Paste this exactly on the web client. No trailing slash. Do not use 127.0.0.1 for the
            redirect.
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
            . Name it flexis-local. Create. Skip billing. Confirm the top bar shows this project.
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
            If the page says Manage, it is already on. Scopes in step 5 will not appear until these APIs
            are enabled on the same project as the client.
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
            . If you see Get started, click it. App name Flexis. User support email: your Gmail.
            Audience: External. Developer contact: your Gmail. Finish.
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
            . Publishing status must stay Testing. Under Test users, add the Gmail you will use on
            Connect Gmail. Save. Google blocks other accounts while the app is in Testing.
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
            . The picker does not list the raw URLs as options. Finish step 2 first. Only enabled APIs
            show scopes.
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
                  <TableCell align="left">Console name</TableCell>
                  <TableCell align="left">URI to paste</TableCell>
                  <TableCell align="left">Kind</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {scopes.map((scope) => (
                  <TableRow key={scope.uri}>
                    <TableCell align="left">
                      {scope.label}
                      <Typography variant="caption" color="text.secondary" component="div">
                        {scope.api}
                      </Typography>
                    </TableCell>
                    <TableCell align="left">{scope.uri}</TableCell>
                    <TableCell align="left">{scope.kind}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <Alert severity="info">
            Gmail modify is restricted. That is expected. Stay on Testing. Do not pick See and download
            all your Gmail, mail.google.com, or See, edit, create, and delete all of your Google Drive
            files.
          </Alert>
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
            Create. Copy Client ID and Client secret. Do not use Desktop, Android, or iOS. Do not use
            127.0.0.1:5080 for the redirect.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            7. Put credentials in Flexis
          </Typography>
          <Typography variant="body2">
            Sign in as an admin. Open{" "}
            <Link component={RouterLink} to={appPaths.settings}>
              Settings
            </Link>
            . Under Google Cloud client, paste Client ID and Client secret. Save. That is one client
            for the Flexis app. Each person still connects their own Gmail on Job Application. Do not
            put the secret in a committed project file.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            8. Confirm Flexis is running
          </Typography>
          <Typography variant="body2">
            Saving the Google Cloud client in Settings does not need a restart. Health must be Healthy
            at{" "}
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
            9. Connect Gmail
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText primary="Open Job Application, Settings tab. Connect Gmail opens Google. Copy URL copies the consent URL for another browser." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Pick the test-user Gmail and Allow. Google returns to Flexis. The chip should read Connected and show that address." />
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
    </Stack>
  );
}
