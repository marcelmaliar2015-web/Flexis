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
import { Panel } from "@/features/help/helpUi";

export function HelpProblemsTab() {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Problems
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Start here when Connect Gmail, sheets, pricing, Dashboard, or Settings does not match what
          you expect.
        </Typography>
      </Stack>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Connect Gmail
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">What you see</TableCell>
                  <TableCell align="left">Fix</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow>
                  <TableCell align="left">gmail.modify or spreadsheets not in the list</TableCell>
                  <TableCell align="left">
                    Enable the APIs on Google setup step 2, then Manually add scopes and paste the URIs
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Connect Gmail stays disabled</TableCell>
                  <TableCell align="left">An admin must save the Google Cloud client on product Settings</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">redirect_uri_mismatch</TableCell>
                  <TableCell align="left">
                    Paste http://localhost:5080/api/google/connections/callback on the web client. Not
                    127.0.0.1 and no trailing slash
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Access blocked</TableCell>
                  <TableCell align="left">Add that Gmail under Test users. Stay on Testing</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">403 API not enabled</TableCell>
                  <TableCell align="left">Repeat Google setup step 2 on the same project as the client</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">google=error after Google</TableCell>
                  <TableCell align="left">Confirm scopes on Google setup step 5 and that Health is Healthy</TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Pipeline, sheets, and price
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary="New, Update, and catalog buttons stay disabled"
                secondary="Connect Gmail on Job Application Settings for this signed-in user."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Update adds nothing"
                secondary="The source location may be empty, every listing may already be on the profile main tab, or companies may match a ban. Read added, skipped, and banned on the success notice."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Invited editors cannot type in Company Name"
                secondary="On the named main tab they can edit Status and Issue only. Numbered log tabs and source tabs are owner-only."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Financial totals are zero"
                secondary="Connect Gmail, confirm listings sit on the named main tab, and set Status to Applied or Interview. Invalid, Expired, Other, and blank Status do not price."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="New pipeline rows ignore the rates you just saved"
                secondary="Defaults apply only to rows created after Save defaults. Edit Apply Rate and Bonus Rate on Financial for rows that already exist."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Cannot delete an admin"
                secondary="The last active admin cannot be demoted, deactivated, or deleted."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="I am not in the users table"
                secondary="Your profile is Your account at the top of product Settings. The table lists other people only. Edit display name and password there. Email and role stay as assigned."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Password save is rejected"
                secondary="A new password must be at least 8 characters and include a letter and a digit. Leave the field blank to keep the current password."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Dashboard numbers stay at zero"
                secondary="Connect Gmail, run Update on Operations, then set Status to Applied or Interview on the named profile main tab. Attention cards on Dashboard name the missing step. Header Google sync refreshes those sheet counts every 3 minutes while this tab is visible."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Sync lamps stay red"
                secondary="Click the header Google sync control to run a full refresh now. Confirm Gmail is connected. Red means the last successful sync is older than 8 minutes or a refresh failed."
              />
            </ListItem>
          </List>
        </Stack>
      </Panel>
    </Stack>
  );
}
