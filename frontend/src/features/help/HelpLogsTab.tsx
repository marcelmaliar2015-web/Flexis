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
import { Panel } from "@/features/help/helpUi";
import { appPaths } from "@/shared/config/paths";

export function HelpLogsTab() {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Activity logs
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Open{" "}
          <Link component={RouterLink} to={appPaths.logs}>
            Logs
          </Link>{" "}
          in the left nav. Job Application and Mail Check activity share one page with separate tabs.
          Both feeds are paged. Dashboard still shows a seven-day Job Application chart and recent
          events.
        </Typography>
      </Stack>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Job Application tab
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary="Day groups"
                secondary="Events on the current page are grouped by calendar day in your local timezone. Each card has the time, an action chip, a category chip, a short summary, and a detailed line."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Filters and pages"
                secondary="All, Pipeline, Catalog, Financial, or Account. Search matches summary, detail, action, and category on the server. Choose how many rows per page, then move through the full history."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Refresh"
                secondary="This feed does not poll. Use Refresh after you work in Job Application, or come back after the tab has already been opened."
              />
            </ListItem>
          </List>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">Category</TableCell>
                  <TableCell align="left">When Flexis writes a log</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow>
                  <TableCell align="left">Pipeline</TableCell>
                  <TableCell align="left">
                    Create, edit, or delete a pipeline row; Delete All; Update and Update All; Forward
                    and Forward All; add, rename, or remove a banned company
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Catalog</TableCell>
                  <TableCell align="left">
                    Create, rename, or delete a profile or source; add, rename, or delete a source
                    location
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Financial</TableCell>
                  <TableCell align="left">
                    Save default rates or change apply and bonus rates on a pipeline row
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Account</TableCell>
                  <TableCell align="left">Connect Gmail or disconnect Gmail</TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </TableContainer>
          <Typography variant="body2" color="text.secondary">
            Failed actions are not written. Opening a list or scanning banned matches does not create
            a log.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Mail Check tab
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary="Table"
                secondary="Sticky columns for when, source, mailbox, from, subject, label, action, duration, and detail. Run summaries appear as highlighted rows."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Filters and pages"
                secondary="Filter by auto or manual source, action, and mailbox. Search subject, sender, mailbox, label, detail, or message id. Rows per page and page controls hit the server."
              />
            </ListItem>
          </List>
        </Stack>
      </Panel>
    </Stack>
  );
}
