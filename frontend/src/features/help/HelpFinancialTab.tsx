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
          Financial prices each Operations row from the profile main tab. Open it after listings have
          a Status of Applied or Interview.
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
                  <TableCell align="left">Total</TableCell>
                  <TableCell align="left">Non-empty listing rows on the named profile main tab</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Applied</TableCell>
                  <TableCell align="left">Rows whose Status is Applied</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Interviews</TableCell>
                  <TableCell align="left">Rows whose Status is Interview</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Apply Rate</TableCell>
                  <TableCell align="left">Price per Applied row. Editable per pipeline row. Saved on blur.</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Bonus Rate</TableCell>
                  <TableCell align="left">Price per Interview row. Editable per pipeline row. Saved on blur.</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Price</TableCell>
                  <TableCell align="left">Applied times apply rate plus interviews times bonus rate, rounded to 2 decimals</TableCell>
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
                secondary="Job Application Settings, Financial defaults. Built-in values are 0.06 apply and 1.5 bonus. New pipeline rows copy those defaults. Changing defaults does not rewrite rates already saved on existing rows."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="All sheets"
                secondary="Sum of price, total, applied, and interviews across every pipeline row on the table."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Selected rows"
                secondary="Check rows, or use the header checkbox, to price a subset. The selected card appears as a dash until at least one row is checked."
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
