import Alert from "@mui/material/Alert";
import Link from "@mui/material/Link";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { Link as RouterLink } from "react-router-dom";
import { Panel } from "@/features/help/helpUi";
import { appPaths } from "@/shared/config/paths";

export function HelpOverviewTab() {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Workspace
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Flexis is a signed-in product shell. Job Application is where listings, sheets, pricing, and
          Gmail live. This Help set matches that workspace.
        </Typography>
      </Stack>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Where to click
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary="Left nav"
                secondary="Dashboard, Job Application, Settings, and Help. Dashboard is the workspace status board. Work happens in Job Application."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Header Google sync"
                secondary="Left of Gmail status. Shows Updated x mins ago with a red, amber, or green lamp bar. Click to refresh all sheets and Job Application configuration now. Auto refresh is every 3 minutes while this tab is visible."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Header Gmail status"
                secondary="Right side of the app bar, left of the account avatar. Shows whether this account's Gmail is connected. Open it for a short status and a link to Job Application, Settings, or Help."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Account menu"
                secondary="Initials avatar in the app bar. Compact header with avatar, name, email, and role, then Settings, Help, and Sign out. Edit name and password on Settings."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Job Application tabs"
                secondary="Operations, Financial, Logs, then Settings. After you open a tab it stays mounted so returning does not flash empty."
              />
            </ListItem>
          </List>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Roles
          </Typography>
          <Typography variant="body2">
            Every signed-in person uses the same Job Application screens. Settings always has Your account.
            Admin is the only role that sees Google Cloud client and other users on product Settings.
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText primary="Admin" secondary="Save the Flexis Google Cloud web client. Create, edit, and delete other users. The last active admin cannot be demoted, deactivated, or deleted." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="User and Viewer" secondary="Same product screens today. Connect their own Gmail on Job Application. Edit their display name and password on Settings. Cannot edit the Flexis Google Cloud client or the users table." />
            </ListItem>
          </List>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            First run
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText primary="1. Admin opens product Settings and saves Client ID and Client secret from Google Cloud. Help Google setup has those steps." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary={
                  <>
                    2. Each person opens{" "}
                    <Link component={RouterLink} to={appPaths.jobApplication}>
                      Job Application
                    </Link>
                    , Settings tab, and Connect Gmail with a Google test user.
                  </>
                }
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="3. Create at least one profile and one source. Flexis makes a Google Sheet for each under Flexis / Job Application / Profiles or Sources." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="4. On Operations, add a pipeline row that pairs a profile with a source location, then use Update to copy listings." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="5. On the profile sheet, set Status to Applied or Interview. Financial prices those rows. Logs records the actions." />
            </ListItem>
          </List>
          <Alert severity="info">
            Catalog and Operations stay disabled until that signed-in user has connected Gmail. Financial
            and Logs still open; sheet counts stay zero until Gmail can read the profile workbook.
          </Alert>
        </Stack>
      </Panel>
    </Stack>
  );
}
