import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
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
import { helpTabItems, type HelpTabValue } from "@/features/help/helpTabs";
import { appPaths } from "@/shared/config/paths";

type HelpOverviewTabProps = {
  onOpenTab: (tab: HelpTabValue) => void;
};

export function HelpOverviewTab({ onOpenTab }: HelpOverviewTabProps) {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          How Flexis is organized
        </Typography>
        <Typography variant="body2" color="text.secondary">
          After sign-in the shell is a header, a left nav, and a scrolling content pane. Job
          Application is where listings, sheets, pricing, and Gmail live. Dashboard shows whether
          that workspace is healthy and producing counts. Settings holds your account. This Help set
          is the product map and the how-to guides.
        </Typography>
      </Stack>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            These guides
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Overview is this map. The other tabs are topic guides. Open one from the tab bar or from
            a row below.
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">Tab</TableCell>
                  <TableCell align="left">What it covers</TableCell>
                  <TableCell align="left">Open</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {helpTabItems
                  .filter((item) => item.value !== "overview")
                  .map((item) => (
                    <TableRow key={item.value}>
                      <TableCell align="left">{item.label}</TableCell>
                      <TableCell align="left">{item.summary}</TableCell>
                      <TableCell align="left">
                        <Button variant="text" onClick={() => onOpenTab(item.value)}>
                          Open
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Left nav
          </Typography>
          <Typography variant="body2" color="text.secondary">
            The four product screens stay in view while content scrolls. Open a row to go there.
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">Screen</TableCell>
                  <TableCell align="left">What you get</TableCell>
                  <TableCell align="left">When to use it</TableCell>
                  <TableCell align="left">Open</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow>
                  <TableCell align="left">Dashboard</TableCell>
                  <TableCell align="left">
                    Live status for this account: platform health, Google Cloud client, Gmail,
                    catalog size, listing and price KPIs, status mix, price by pipeline row, setup
                    attention, seven-day activity, and Admin user counts.
                  </TableCell>
                  <TableCell align="left">
                    See what is blocked, what the sheets currently count, and what to do next.
                    Listing counts stay zero until Gmail can read profile workbooks.
                  </TableCell>
                  <TableCell align="left">
                    <Link component={RouterLink} to={appPaths.dashboard}>
                      Open
                    </Link>
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Job Application</TableCell>
                  <TableCell align="left">
                    Operations pipeline, Financial pricing, Logs, and Settings for Gmail, default
                    rates, profiles, sources, and locations.
                  </TableCell>
                  <TableCell align="left">
                    Create sheets, pair a profile with a source location, copy listings, set Status,
                    and price Applied and Interview rows.
                  </TableCell>
                  <TableCell align="left">
                    <Link component={RouterLink} to={appPaths.jobApplication}>
                      Open
                    </Link>
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Settings</TableCell>
                  <TableCell align="left">
                    Your account: display name and optional password. Email and role stay as
                    assigned. Admin also sees Google Cloud client and other users. You are not a row
                    in that users table.
                  </TableCell>
                  <TableCell align="left">
                    Change your name or password. Admins save the Flexis Google Cloud web client and
                    manage other accounts.
                  </TableCell>
                  <TableCell align="left">
                    <Link component={RouterLink} to={appPaths.settings}>
                      Open
                    </Link>
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Help</TableCell>
                  <TableCell align="left">This guide set.</TableCell>
                  <TableCell align="left">Learn the product, set up Google, or fix a problem.</TableCell>
                  <TableCell align="left">This page</TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Header
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary="Flexis brand"
                secondary="Left side of the app bar. Goes to Dashboard when you are signed in."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Google sync"
                secondary="Left of Gmail status. Updated x mins ago plus a red, amber, or green lamp bar. Green is under 2 minutes, amber under 8, red after that or on failure. Lamps chase while a sync runs. Click to refresh sheets and Job Application configuration now. Auto refresh is every 3 minutes while this browser tab is visible."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Gmail status"
                secondary="Left of the account avatar. Live, waiting, or idle orb plus Gmail and the connected address. Open it for a short status and a link to Job Application, Settings, or Help. Connect and disconnect stay on Job Application Settings."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="Account menu"
                secondary="Initials avatar. Compact header with avatar, name, email, and a role chip, then Settings, Help, and Sign out. Edit name and password on Settings, not in this menu."
              />
            </ListItem>
          </List>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            Job Application tabs
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Open{" "}
            <Link component={RouterLink} to={appPaths.jobApplication}>
              Job Application
            </Link>
            . After a tab is opened it stays mounted so returning does not flash empty.
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">Tab</TableCell>
                  <TableCell align="left">What it does</TableCell>
                  <TableCell align="left">Guide</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow>
                  <TableCell align="left">Operations</TableCell>
                  <TableCell align="left">
                    Pipeline table. Each row pairs a profile with a source location. Update copies
                    listings. Forward archives the profile main tab. A row click opens banned
                    companies and live matches.
                  </TableCell>
                  <TableCell align="left">
                    <Button variant="text" onClick={() => onOpenTab("operations")}>
                      Operations
                    </Button>
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Financial</TableCell>
                  <TableCell align="left">
                    Prices each pipeline row from Applied and Interview counts on the profile main
                    tab. Edit rates on the row. Totals for all sheets and for selected rows.
                  </TableCell>
                  <TableCell align="left">
                    <Button variant="text" onClick={() => onOpenTab("financial")}>
                      Financial
                    </Button>
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Logs</TableCell>
                  <TableCell align="left">
                    Searchable, category-filtered activity feed grouped by day. Newest 200 events
                    for this account.
                  </TableCell>
                  <TableCell align="left">
                    <Button variant="text" onClick={() => onOpenTab("logs")}>
                      Logs
                    </Button>
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell align="left">Settings</TableCell>
                  <TableCell align="left">
                    Connect Gmail, default apply and bonus rates, profile and source tables, and
                    source location tabs. Creating a profile or source creates a Google Sheet under
                    Flexis / Job Application / Profiles or Sources.
                  </TableCell>
                  <TableCell align="left">
                    <Button variant="text" onClick={() => onOpenTab("google")}>
                      Google setup
                    </Button>
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
            Roles
          </Typography>
          <Typography variant="body2">
            Every signed-in person uses the same Dashboard, Job Application, and Help screens. Your
            account on product Settings is available to every role.
          </Typography>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary="Admin"
                secondary="Save the Flexis Google Cloud web client. Create, edit, and delete other users. The last active admin cannot be demoted, deactivated, or deleted. The users table does not include your own account."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="User and Viewer"
                secondary="Same product screens today. Connect their own Gmail on Job Application. Edit display name and password on Settings. Cannot edit the Flexis Google Cloud client or the users table."
              />
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
              <ListItemText
                primary="1. Admin saves the Google Cloud client"
                secondary="On product Settings, paste Client ID and Client secret. Google setup has those Google Cloud steps."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="2. Each person connects Gmail"
                secondary="Job Application, Settings tab. Connect Gmail with a Google test user. Copy URL copies the consent URL for another browser."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="3. Create a profile and a source"
                secondary="Still on Job Application Settings. Flexis makes a Google Sheet for each under Flexis / Job Application / Profiles or Sources, then shows that URL."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="4. Pair them on Operations"
                secondary="Add a pipeline row that pairs a profile with a source location, then Update to copy listings onto the profile main tab."
              />
            </ListItem>
            <ListItem disableGutters>
              <ListItemText
                primary="5. Set Status and read price"
                secondary="On the named profile main tab, set Status to Applied or Interview. Financial prices those counts. Dashboard shows the same totals. Logs records the actions."
              />
            </ListItem>
          </List>
          <Alert severity="info">
            Catalog and Operations stay disabled until this signed-in user has connected Gmail.
            Financial, Logs, and Dashboard still open. Sheet counts stay zero until Gmail can read
            the profile workbook. Dashboard attention cards point at the next missing step.
          </Alert>
        </Stack>
      </Panel>
    </Stack>
  );
}
