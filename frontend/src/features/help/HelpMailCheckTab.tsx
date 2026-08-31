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

export function HelpMailCheckTab() {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Mail Check
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Auto-triage Gmail for interview work. Connect Gmail on Mail Check Settings. Outlook support
          is planned next. The only cost is your OpenAI API key, which you paste on Mail Check
          Settings.
        </Typography>
      </Stack>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Tabs
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">Tab</TableCell>
                  <TableCell align="left">What it does</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow>
                  <TableCell align="left">Inbox</TableCell>
                  <TableCell align="left">
                    Mail Flexis labeled and pinned. Filter by Interview Scheduled, Waiting for
                    answer, Need to Schedule/Availability, or Others.
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Check</TableCell>
                  <TableCell align="left">
                    Check now, last-run counts, and why each message was labeled, trashed, or left
                    alone. Auto-check runs about every two minutes while this browser tab is visible.
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Settings</TableCell>
                  <TableCell align="left">
                    Mailbox connect for Gmail, plus OpenAI key and model. The key is never shown again
                    after save. Pick any chat or reasoning model; Flexis adapts the request.
                  </TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            What Flexis does
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary="Creates four Gmail labels"
                secondary="Interview Scheduled, Waiting for answer, Need to Schedule/Availability, and Others. Created when you save a key with Gmail connected, and on each check."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Reads inbox, spam, and other categories"
                secondary="Promotions, updates, forums, and social are included. Real recruiter mail often lands there."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Pins keepers"
                secondary="Labeled interview mail is starred. Mail found in spam is moved back to the inbox."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Trashes noise only"
                secondary="Application-received receipts, mass rejections, and job-board alerts go to trash. Personal mail is never trashed."
              />
            </ListItem>
          </List>
        </Stack>
      </Panel>
    </Stack>
  );
}
