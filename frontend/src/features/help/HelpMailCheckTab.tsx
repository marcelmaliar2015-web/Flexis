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

import type { HelpTabValue } from "@/features/help/helpTabs";

import { appPaths } from "@/shared/config/paths";



type HelpMailCheckTabProps = {

  onOpenTab?: (tab: HelpTabValue) => void;

};



export function HelpMailCheckTab({ onOpenTab }: HelpMailCheckTabProps) {

  return (

    <Stack spacing={2}>

      <Stack spacing={0.5}>

        <Typography variant="h6" component="h2">

          Mail Check

        </Typography>

        <Typography variant="body2" color="text.secondary">

          Auto-triage Gmail or Outlook for interview work. Connect any number of mailboxes on Mail Check Settings.

          Outlook uses free Microsoft Graph app registration and OAuth, not a paid Graph mail plan.

          Classification uses your OpenAI API key.

        </Typography>

      </Stack>

      <Panel>

        <Stack spacing={1.5}>

          <Typography variant="h6" component="h2">

            Before you start

          </Typography>

          <List disablePadding>

            <ListItem disableGutters>

              <ListItemText

                primary="Gmail"

                secondary="Uses the Flexis Google Cloud client. An admin saves it on product Settings. Each user connects Gmail on Mail Check Settings."

              />

            </ListItem>

            <ListItem disableGutters>

              <ListItemText

                primary="Outlook"

                secondary="Uses a free Azure app registration and Microsoft Graph OAuth. There is no paid Graph mail plan. An admin saves Application ID and client secret on product Settings. Each user connects Outlook on Mail Check Settings."

              />

            </ListItem>

            <ListItem disableGutters>

              <ListItemText

                primary="OpenAI key"

                secondary="Required before auto-check classifies mail. Saved encrypted on the API. Never shown again after save."

              />

            </ListItem>

          </List>

          {onOpenTab ? (

            <Stack direction="row" spacing={1}>

              <Button variant="text" onClick={() => onOpenTab("google")}>

                Google setup

              </Button>

              <Button variant="text" onClick={() => onOpenTab("microsoft")}>

                Microsoft setup

              </Button>

            </Stack>

          ) : null}

        </Stack>

      </Panel>

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
                    Mail Flexis labeled or categorized and pinned. Filter by Rejected, Applied,
                    Schedule, Scheduled, Assessment, Availability, Success, Other, or Less Important
                    (pin-configured labels only).
                  </TableCell>

                </TableRow>

                <TableRow>

                  <TableCell align="left">Check</TableCell>

                  <TableCell align="left">

                    Check all processes every inbox and junk candidate one message at a time with
                    live progress. Background auto-check still runs about every two minutes while
                    Mail Check is open.

                  </TableCell>

                </TableRow>

                <TableRow>

                  <TableCell align="left">Settings</TableCell>

                  <TableCell align="left">

                    Connect Gmail or Outlook, OpenAI key and model, label actions, and the classifier
                    prompt. Pick any chat or reasoning model; Flexis adapts the request.

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

                primary="Creates pin labels"

                secondary="Gmail labels or Outlook master categories for labels you configure as Pin. Created when you save settings with a mailbox connected, and on each check."

              />

            </ListItem>

            <ListItem disableGutters>

              <ListItemText

                primary="Reads inbox and junk or spam"

                secondary="Gmail also reads promotions, updates, forums, and social. Real recruiter mail often lands there."

              />

            </ListItem>

            <ListItem disableGutters>

              <ListItemText

                primary="Pins keepers"

                secondary="Gmail stars pinned mail. Outlook pins mail to the top of the inbox and adds a category. Junk or spam keepers move back to the inbox."

              />

            </ListItem>

            <ListItem disableGutters>

              <ListItemText

                primary="Classifies then acts"

                secondary="OpenAI returns one label per message. Settings choose pin, trash, or keep for each label."

              />

            </ListItem>

            <ListItem disableGutters>

              <ListItemText

                primary="Trashes on request"

                secondary="Labels configured as Trash move mail to trash. Keep leaves messages untouched."

              />

            </ListItem>

          </List>

        </Stack>

      </Panel>

      <Panel>

        <Stack spacing={1.5}>

          <Typography variant="h6" component="h2">

            Connect flow

          </Typography>

          <List disablePadding>

            <ListItem disableGutters>

              <ListItemText primary="Open Mail Check, Settings tab." />

            </ListItem>

            <ListItem disableGutters>

              <ListItemText primary="Add Gmail or Add Outlook. Connect as many accounts as you need." />

            </ListItem>

            <ListItem disableGutters>

              <ListItemText primary="Sign in and accept permissions. Flexis returns to Mail Check with the mailbox in the list." />

            </ListItem>

            <ListItem disableGutters>

              <ListItemText primary="Paste an OpenAI API key, pick a model, and Save." />

            </ListItem>

            <ListItem disableGutters>

              <ListItemText

                primary={

                  <Link component={RouterLink} to={appPaths.mailCheck}>

                    Open Mail Check

                  </Link>

                }

              />

            </ListItem>

          </List>

          <Alert severity="info">

            Mail Check mailbox is separate from Job Application Gmail used for Sheets and pipeline.

          </Alert>

        </Stack>

      </Panel>

    </Stack>

  );

}

