import Alert from "@mui/material/Alert";
import Link from "@mui/material/Link";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type { ReactNode } from "react";
import { Link as RouterLink } from "react-router-dom";
import { ExternalLink, Panel, RedirectUriBlock, outlookRedirectUri } from "@/features/help/helpUi";
import { appPaths } from "@/shared/config/paths";

const permissionNames = [
  "User.Read",
  "Mail.ReadWrite",
  "MailboxSettings.ReadWrite",
  "openid",
  "profile",
  "email",
  "offline_access",
] as const;

function StepActions({ items }: { items: ReactNode[] }) {
  return (
    <List disablePadding>
      {items.map((item, index) => (
        <ListItem key={index} disableGutters sx={{ alignItems: "flex-start" }}>
          <ListItemText
            primary={
              <Typography variant="body2" component="div">
                {index + 1}. {item}
              </Typography>
            }
          />
        </ListItem>
      ))}
    </List>
  );
}

export function HelpMicrosoftTab() {
  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Microsoft setup
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Follow steps 1 to 8 in order. Do not skip ahead. Steps 1 to 5 are in Azure. Step 6 is
          Flexis Settings. Step 8 connects your Outlook mailbox in Mail Check.
        </Typography>
      </Stack>
      <Alert severity="info">
        Microsoft Graph Outlook mail is not a paid or metered mail API. Flexis does not ask you to
        buy a Microsoft Graph plan. You only register a free app, sign in with OAuth, and use your
        own OpenAI key for classification.
      </Alert>
      <Alert severity="info">
        Goal: unlock Connect Outlook. Steps 1 to 5 are free Azure app setup. Step 6 pastes Application
        ID and secret into Flexis Settings. Step 8 connects your Outlook mailbox on Mail Check.
      </Alert>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            1. Open a directory in Azure Portal
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Goal: App registrations must run inside a directory. This is free account setup with
            Microsoft, not a Graph payment plan. Skip this tab if you use Gmail only.
          </Typography>
          <Typography variant="subtitle2">Do this first (no payment setup)</Typography>
          <StepActions
            items={[
              <>
                Open{" "}
                <ExternalLink href="https://portal.azure.com/">https://portal.azure.com/</ExternalLink>{" "}
                and sign in with the same Microsoft account you use for Outlook.
              </>,
              "If the top bar already shows a directory name, you are done with step 1. Go to step 2.",
              "If New registration works on App registrations, you are done with step 1. Go to step 2.",
              <>
                If New registration is blocked with “applications outside of a directory has been
                deprecated,” create a free directory with{" "}
                <ExternalLink href="https://developer.microsoft.com/microsoft-365/dev-program">
                  Microsoft 365 Developer Program
                </ExternalLink>
                {" "}
                join (Contact Email, Country/Region, Company such as Individual, preferences, Join).
                No Graph payment. If Set up E5 subscription appears and you want it, follow Microsoft’s
                screens; if it says you do not qualify, stay signed in and retry App registrations, or
                use the last option below.
              </>,
              "If another Flexis admin already saved the Microsoft client on Settings, skip steps 1 to 6. Open Mail Check Settings and Add Outlook only.",
            ]}
          />
          <Typography variant="subtitle2">Only if Microsoft still blocks New registration</Typography>
          <Typography variant="body2" color="text.secondary">
            Some personal accounts must create an Azure directory through Microsoft’s free Azure
            signup. That is still not a Graph API purchase. If Microsoft’s own form asks for a card,
            that is Microsoft account verification. Flexis does not require it and does not charge
            you. Prefer finishing without that form whenever Portal already shows a directory.
          </Typography>
          <StepActions
            items={[
              <>
                Open{" "}
                <ExternalLink href="https://azure.microsoft.com/free/">
                  https://azure.microsoft.com/free/
                </ExternalLink>
                {" "}
                only if steps above still cannot open App registrations.
              </>,
              "Complete Microsoft’s free signup screens with your Outlook Microsoft account.",
              "If Microsoft asks for a card, that is their verification form, not a Flexis or Graph fee. You can stop if you refuse; then Connect Outlook cannot be unlocked until a directory exists.",
              "After signup, open Azure Portal, select the new directory in the top bar, then continue to step 2.",
            ]}
          />
          <Typography variant="body2">
            Done when: Azure Portal top bar shows a directory, or New registration opens. Then go to
            step 2.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            2. Register an app
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Goal: create one Azure app named flexis-local and copy its Application (client) ID.
          </Typography>
          <Typography variant="subtitle2">Do this</Typography>
          <StepActions
            items={[
              <>
                Open{" "}
                <ExternalLink href="https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade">
                  App registrations
                </ExternalLink>
                .
              </>,
              "Click New registration.",
              "If Azure shows the directory-deprecated message, go back to step 1. Do not continue.",
              "Name: flexis-local.",
              "Supported account types: Accounts in any organizational directory and personal Microsoft accounts.",
              "Redirect URI: leave empty for now.",
              "Click Register.",
              "On Overview, copy Application (client) ID. Keep it for step 6.",
            ]}
          />
          <Typography variant="body2">
            Done when: you have an Application (client) ID and the app Overview page is open.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            3. Create a client secret
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Goal: create a secret Value for that same app. Azure shows the Value only once.
          </Typography>
          <Typography variant="subtitle2">Do this</Typography>
          <StepActions
            items={[
              "Stay on the same app from step 2.",
              "Left menu: Certificates & secrets.",
              "Client secrets → New client secret.",
              "Description: flexis-local. Choose an expiry. Click Add.",
              "Copy the Value column immediately. Do not copy Secret ID.",
              "Paste the Value somewhere safe for step 6.",
            ]}
          />
          <Typography variant="body2">
            Done when: you have both Application (client) ID and the secret Value.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            4. Add the redirect URI
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Goal: tell Azure where to send the browser after Outlook sign-in.
          </Typography>
          <Typography variant="subtitle2">Do this</Typography>
          <StepActions
            items={[
              "Stay on the same app from step 2.",
              "Left menu: Authentication.",
              "Add a platform → Web.",
              "Paste this Redirect URI exactly. No trailing slash. Do not use 127.0.0.1.",
            ]}
          />
          <RedirectUriBlock uri={outlookRedirectUri} />
          <StepActions
            items={[
              "Leave Implicit grant and hybrid flows unchecked.",
              "Click Configure, then Save if Azure asks.",
            ]}
          />
          <Typography variant="body2">
            Done when: Authentication shows that Web redirect URI.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            5. Add API permissions
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Goal: allow this app to read and organize mail for the signed-in user.
          </Typography>
          <Typography variant="subtitle2">Do this</Typography>
          <StepActions
            items={[
              "Stay on the same app from step 2.",
              "Left menu: API permissions.",
              "Add a permission → Microsoft Graph → Delegated permissions.",
              <>
                Search and check each of these: {permissionNames.join(", ")}.
              </>,
              "Click Add permissions.",
              "If your tenant shows Grant admin consent, click it. Personal Microsoft accounts often skip this.",
              "Do not add Application permissions.",
            ]}
          />
          <Typography variant="body2">
            Done when: API permissions lists those Microsoft Graph delegated permissions.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            6. Paste credentials into Flexis
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Goal: save Application ID and secret in Flexis Settings so Connect Outlook can turn on.
          </Typography>
          <Typography variant="subtitle2">Do this</Typography>
          <StepActions
            items={[
              "Sign in to Flexis as an admin.",
              <>
                Open{" "}
                <Link component={RouterLink} to={appPaths.settings}>
                  Settings
                </Link>
                .
              </>,
              "Find Microsoft client.",
              "Paste Application (client) ID from step 2.",
              "Paste secret Value from step 3.",
              "Click Save.",
            ]}
          />
          <Typography variant="body2">
            Done when: Settings shows the Microsoft client saved. This is not your mailbox yet.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            7. Confirm Flexis is running
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Goal: API and frontend are up before you connect Outlook.
          </Typography>
          <Typography variant="subtitle2">Do this</Typography>
          <StepActions
            items={[
              <>
                Open{" "}
                <ExternalLink href="http://localhost:5080/api/health">
                  http://localhost:5080/api/health
                </ExternalLink>
                . It must say Healthy.
              </>,
              <>
                Open{" "}
                <ExternalLink href="http://127.0.0.1:5173/">http://127.0.0.1:5173/</ExternalLink>.
              </>,
              "No restart is needed after saving the Microsoft client.",
            ]}
          />
          <Typography variant="body2">
            Done when: health is Healthy and the app loads.
          </Typography>
        </Stack>
      </Panel>
      <Panel>
        <Stack spacing={1.5}>
          <Typography variant="h6" component="h2">
            8. Connect your Outlook mailbox
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Goal: link the mailbox Flexis should organize. This is the real mail connection.
          </Typography>
          <Typography variant="subtitle2">Do this</Typography>
          <StepActions
            items={[
              <>
                Open{" "}
                <Link component={RouterLink} to={appPaths.mailCheck}>
                  Mail Check
                </Link>
                .
              </>,
              "Open the Settings tab.",
              "Click Add Outlook. If the button is disabled, step 6 is not finished.",
              "Sign in with the Outlook or Microsoft 365 mailbox you want Flexis to triage.",
              "Click Accept.",
              "Wait until Mail Check shows Connected and your address.",
              "On the same Settings tab, paste an OpenAI API key, pick a model, and Save.",
            ]}
          />
          <Typography variant="body2">
            Done when: mailbox chip is Connected and an OpenAI key is saved. Flexis can then create
            categories and run Mail Check.
          </Typography>
        </Stack>
      </Panel>
    </Stack>
  );
}
