import Alert from "@mui/material/Alert";
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

export function HelpFinancialTab() {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Financial
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Financial prices each Operations row from the profile main tab and numbered archive tabs.
          Today is the current main tab. Archived is sheets named 1, 2, 3 after Forward. Lifetime is
          both combined. Dashboard workspace KPIs still use today main-tab counts.
        </Typography>
      </Stack>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            What each column means
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">Column</TableCell>
                  <TableCell align="left">Meaning</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow>
                  <TableCell align="left">Profile</TableCell>
                  <TableCell align="left">Profile title for that pipeline row</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Source</TableCell>
                  <TableCell align="left">Source title · location</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Today</TableCell>
                  <TableCell align="left">Applied, interviews, and price from the named profile main tab</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Archived</TableCell>
                  <TableCell align="left">Applied, interviews, and price from numbered sheets created by Forward</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Lifetime</TableCell>
                  <TableCell align="left">Today plus archived combined for that row</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Apply rate</TableCell>
                  <TableCell align="left">Price per Applied row. Editable per pipeline row. Saved on blur.</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Bonus rate</TableCell>
                  <TableCell align="left">Price per Interview row. Editable per pipeline row. Saved on blur.</TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Defaults and selection
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary="Default rates"
                secondary="Settings (Job Application), Financial defaults. Built-in values are 0.06 apply and 1.5 bonus. New pipeline rows copy those defaults. Changing defaults does not rewrite rates already saved on existing rows."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Summary cards"
                secondary="Today, Archived, and Lifetime show workspace price and listing counts for every pipeline row."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Performance graph"
                secondary="Hourly snapshots of today, archived, and lifetime price with connected lines and hover detail. Switch to Daily to plot the last snapshot of each day. Opening Financial or Google workspace sync writes or updates this hour in PostgreSQL."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Selected rows"
                secondary="Check rows, or use the header checkbox, to price a subset. The selected card shows today, archived, and lifetime totals."
              />
            </ListItem>
          </List>
          <Alert severity="info">
            Invalid, Expired, Other, and blank Status count toward Total only. They do not add to
            Applied, Interviews, or Price. Gmail is not required to open Financial; counts stay zero
            until Flexis can read the profile sheet.
          </Alert>
        </Stack>
      </Panel>
    </Stack>
  );
}
