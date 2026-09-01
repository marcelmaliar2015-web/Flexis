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
import { Panel } from "@/features/help/helpUi";
import { appPaths } from "@/shared/config/paths";

export function HelpOperationsTab() {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Operations
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Operations is the pipeline. Each row pairs one profile workbook with one source location
          tab. Update copies listings onto the profile main tab. Forward archives that tab as a
          numbered log and opens a new empty main tab.
        </Typography>
      </Stack>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Pipeline table
          </Typography>
          <Typography variant="body2">
            Open{" "}
            <Link component={RouterLink} to={appPaths.jobApplication}>
              Job Application
            </Link>
            , Operations. Columns are Profile and Source (source title · location). The same profile
            and source location cannot be added twice.
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText primary="New" secondary="Choose a profile and a source location. Gmail must be connected. At least one profile and one source location must exist." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Update All" secondary="Runs Update on every pipeline row. Copies new listings from each paired source location onto that profile main tab." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Forward All" secondary="Runs Forward once per distinct profile, even if several rows share that profile." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Delete All" secondary="Removes every pipeline pairing for this account. Listings already on profile sheets stay." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Row click" secondary="Opens the pipeline entry page for that pairing." />
            </ListItem>
          </List>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Pipeline entry
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText primary="Update" secondary="Copies Company Name, Position, Link, and JD from the source location onto the named profile main tab. Skips rows already on that tab (same company, position, and link). Skips banned companies. Result counts added, skipped, and banned." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Forward" secondary="Renames the current main tab to the next unused positive integer (1, 2, 3, …) and creates a new empty main tab with the original profile name. Numbered tabs are locked logs. Update always writes to the named main tab, never a numbered tab." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Delete" secondary="Removes this pairing only. The Google Sheets stay." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Profile info" secondary="Edit the paired profile's Profile tab fields on the pipeline entry page." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Banned companies" secondary="Per profile. Update will not copy a listing whose company matches a ban. The page lists live matches on the profile main tab and sets Status to Banned on those rows, refreshed on the Google workspace sync interval (3 minutes)." />
            </ListItem>
          </List>
          <Alert severity="info">
            folds case and punctuation, strips legal suffixes and generic words like Solutions or
            Group, and matches when any normalized key overlaps. Duplicate bans that match an existing
            name are rejected.
          </Alert>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Profiles, sources, locations
          </Typography>
          <Typography variant="body2">
            Job Application Settings holds Gmail, default rates, profiles, sources, and source
            location tabs. Creating a profile or source creates a Google Sheet and then shows that
            URL.
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText primary="Profile" secondary="Main tab name is the profile title. Columns: Company Name, Position, Link, JD, Download, Status, Issue. Status is Applied, Interview, Banned, Invalid, Expired, or Other, each with its own color." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Source" secondary="First location tab is US. Further tabs are locations. Source tabs have Company Name, Position, Link, and JD. No Status column." />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText primary="Locations" secondary="Rename or add tabs on the source workbook. A source must keep at least one location. Duplicate location names are rejected." />
            </ListItem>
          </List>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Sheet lock
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">Who</TableCell>
                  <TableCell align="left">Named profile main tab</TableCell>
                  <TableCell align="left">Numbered log tabs and source tabs</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow>
                  <TableCell align="left">Connected Google owner</TableCell>
                  <TableCell align="left">Every cell</TableCell>
                  <TableCell align="left">Every cell</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Invited editors</TableCell>
                  <TableCell align="left">Status and Issue only</TableCell>
                  <TableCell align="left">Owner only</TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </TableContainer>
          <Typography variant="body2" color="text.secondary">
            Tabs use a fixed 21 pixel row height, black body text, and wrap. The header stays navy
            with light text. Update and Forward reapply this lock.
          </Typography>
        </Stack>
      </Panel>
    </Stack>
  );
}
