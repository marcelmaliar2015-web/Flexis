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

export function HelpLogsTab() {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Activity log
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Logs is a dated record of Job Application work for this account. Newest events appear first.
          Flexis keeps the latest 200. Dashboard shows a seven-day chart and the newest events; this
          tab is the full feed.
        </Typography>
      </Stack>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            How to read it
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText primary="Day groups" secondary="Events are grouped by calendar day in your local timezone. Each card has the time, an action chip, a category chip, a short summary, and a detailed line." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Filters" secondary="All, Pipeline, Catalog, Financial, or Account. Search matches summary, detail, action, and category." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Refresh" secondary="Logs do not poll. Use Refresh after you work in another tab, or come back after the tab has already been opened." />
            </ListItem>
          </List>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            What is recorded
          </Typography>
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
                  <TableCell align="left">Save default rates or change apply and bonus rates on a pipeline row</TableCell>
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
    </Stack>
  );
}
