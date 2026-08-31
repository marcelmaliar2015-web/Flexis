import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Grid from "@mui/material/Grid";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { Link as RouterLink } from "react-router-dom";
import {
  activityByDay,
  attentionItems,
  formatCount,
  formatOptionalCount,
  formatOptionalPrice,
  formatPercent,
  formatPrice,
  formatRate,
  formatWhen,
  locationCount,
  priceBars,
  queryErrorMessage,
  statusMix,
  summarizeUsers,
} from "@/features/dashboard/dashboardStats";
import {
  DayBar,
  DayChart,
  DayColumn,
  KpiCard,
  MixSegment,
  MixTrack,
  Panel,
  PriceFill,
  PriceTrack,
  ProgressBar,
} from "@/features/dashboard/dashboardUi";
import { appPaths } from "@/shared/config/paths";
import type { JobApplicationLog, JobFinancialBoard } from "@/shared/types/jobApplication";
import type { GoogleConnectionStatus } from "@/shared/types/google";
import type { HealthStatusDto } from "@/shared/types/health";
import type { JobPipelineBoard } from "@/shared/types/pipeline";
import type { UserDto } from "@/shared/types/user";
import { queryCount } from "@/shared/api/queryState";

const categoryLabels: Record<string, string> = {
  pipeline: "Pipeline",
  catalog: "Catalog",
  financial: "Financial",
  account: "Account",
};

type DashboardBoardProps = {
  isAdmin: boolean;
  health: HealthStatusDto | undefined;
  healthError: unknown;
  google: GoogleConnectionStatus | undefined;
  googleError: unknown;
  pipeline: JobPipelineBoard | undefined;
  pipelineLoading: boolean;
  pipelineError: unknown;
  financial: JobFinancialBoard | undefined;
  financialLoading: boolean;
  financialError: unknown;
  logs: JobApplicationLog[] | undefined;
  logsLoading: boolean;
  logsError: unknown;
  users: UserDto[] | undefined;
  usersLoading: boolean;
  usersError: unknown;
};

export function DashboardBoard(props: DashboardBoardProps) {
  const mix = statusMix(props.financial);
  const prices = priceBars(props.financial?.rows);
  const days = activityByDay(props.logs);
  const attention = attentionItems({
    health: props.health,
    google: props.google,
    pipeline: props.pipeline,
    financial: props.financial,
    isAdmin: props.isAdmin,
  });
  const users = summarizeUsers(props.users);
  const recent = props.logsLoading ? [] : (props.logs ?? []).slice(0, 8);
  const profileCount = queryCount(props.pipeline, props.pipelineLoading, props.pipeline?.profiles.length);
  const sourceCount = queryCount(props.pipeline, props.pipelineLoading, props.pipeline?.sources.length);
  const locations = props.pipelineLoading ? null : locationCount(props.pipeline);
  const pipelineRows = queryCount(props.pipeline, props.pipelineLoading, props.pipeline?.entries.length);
  const workspacePrice = queryCount(props.financial, props.financialLoading, props.financial?.allPrice);
  const listingTotal = queryCount(props.financial, props.financialLoading, props.financial?.allTotal);
  const appliedTotal = queryCount(props.financial, props.financialLoading, props.financial?.allApplied);
  const interviewTotal = queryCount(props.financial, props.financialLoading, props.financial?.allInterviews);

  return (
    <Stack spacing={3}>
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 3 }}>
          <StatusCard
            label="Platform"
            value={props.health?.status ?? (props.healthError ? "Unavailable" : "Checking")}
            detail={
              props.healthError
                ? queryErrorMessage(props.healthError) ?? "See Issues in the header."
                : props.health
                  ? props.health.checks.map((check) => `${check.name} ${check.status}`).join(" · ")
                  : "Reading API, PostgreSQL, and MongoDB."
            }
            tone={
              props.health?.status === "Healthy" ? "success" : props.health || props.healthError ? "warning" : "default"
            }
            to={appPaths.health}
            action="Health"
          />
        </Grid>
        <Grid size={{ xs: 12, md: 3 }}>
          <StatusCard
            label="Google Cloud"
            value={
              props.google?.configured
                ? "Configured"
                : props.googleError
                  ? "Unavailable"
                  : props.google
                    ? "Not saved"
                    : "Checking"
            }
            detail={
              props.googleError
                ? queryErrorMessage(props.googleError) ?? "See Issues in the header."
                : props.google?.configured
                  ? "The Flexis web client is ready for Gmail connect."
                  : props.google
                    ? "An admin must save Client ID and secret before anyone can connect Gmail."
                    : "Reading Google Cloud client state."
            }
            tone={props.google?.configured ? "success" : props.google || props.googleError ? "warning" : "default"}
            to={props.isAdmin ? appPaths.settings : appPaths.help}
            action={props.isAdmin ? "Settings" : "Help"}
          />
        </Grid>
        <Grid size={{ xs: 12, md: 3 }}>
          <StatusCard
            label="Gmail"
            value={
              props.google?.connected
                ? (props.google.googleEmail ?? "Connected")
                : props.googleError
                  ? "Unavailable"
                  : props.google
                    ? "Not connected"
                    : "Checking"
            }
            detail={
              props.googleError
                ? queryErrorMessage(props.googleError) ?? "See Issues in the header."
                : props.google?.connected
                  ? props.google.connectedAt
                    ? `Connected ${formatWhen(props.google.connectedAt)}. Sheet counts refresh with header Google sync.`
                    : "Sheet counts refresh with header Google sync."
                  : props.google
                    ? "Catalog and Operations stay disabled until this account connects Gmail."
                    : "Reading Gmail connection state."
            }
            tone={props.google?.connected ? "success" : props.google || props.googleError ? "warning" : "default"}
            to={appPaths.jobApplication}
            action="Job Application"
          />
        </Grid>
        <Grid size={{ xs: 12, md: 3 }}>
          <StatusCard
            label="Catalog"
            value={
              profileCount === null || sourceCount === null
                ? "Loading…"
                : `${formatOptionalCount(profileCount)} / ${formatOptionalCount(sourceCount)}`
            }
            detail={
              locations === null || pipelineRows === null
                ? "Reading profiles, sources, and pipeline rows."
                : `${formatOptionalCount(locations)} source locations · ${formatOptionalCount(pipelineRows)} pipeline rows. Profiles and sources are Google Sheets under Flexis / Job Application.`
            }
            tone={
              profileCount !== null && sourceCount !== null && profileCount > 0 && sourceCount > 0
                ? "success"
                : "default"
            }
            to={appPaths.jobApplication}
            action="Workspace"
          />
        </Grid>
      </Grid>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <KpiCard>
            <Typography variant="overline" color="text.secondary">
              Workspace price
            </Typography>
            <Typography variant="h4">{formatOptionalPrice(workspacePrice)}</Typography>
            <Typography variant="body2" color="text.secondary">
              {props.financialLoading
                ? "Reading profile main tabs."
                : "Applied times apply rate plus interviews times bonus rate, from each profile main tab."}
            </Typography>
          </KpiCard>
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <KpiCard>
            <Typography variant="overline" color="text.secondary">
              Listings
            </Typography>
            <Typography variant="h4">{formatOptionalCount(listingTotal)}</Typography>
            <Typography variant="body2" color="text.secondary">
              {props.financialLoading
                ? "Reading profile main tabs."
                : "Non-empty rows on named profile main tabs. Zero until Gmail can read those workbooks."}
            </Typography>
          </KpiCard>
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <KpiCard>
            <Typography variant="overline" color="text.secondary">
              Applied
            </Typography>
            <Typography variant="h4">{formatOptionalCount(appliedTotal)}</Typography>
            <Typography variant="body2" color="text.secondary">
              {formatPercent(mix.appliedShare)} of listings. Status Applied on the profile main tab.
            </Typography>
          </KpiCard>
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <KpiCard>
            <Typography variant="overline" color="text.secondary">
              Interviews
            </Typography>
            <Typography variant="h4">{formatOptionalCount(interviewTotal)}</Typography>
            <Typography variant="body2" color="text.secondary">
              {formatPercent(mix.interviewShare)} of listings. Status Interview on the profile main tab.
            </Typography>
          </KpiCard>
        </Grid>
      </Grid>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, lg: 7 }}>
          <Panel>
            <Stack spacing={2}>
              <Stack spacing={0.5}>
                <Typography variant="h6" component="h2">
                  Listing status
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Open rows still need a Status. Applied and Interview are priced separately, not a sequential funnel.
                </Typography>
              </Stack>
              <MixTrack aria-hidden="true">
                <MixSegment tone="open" share={mix.openShare} />
                <MixSegment tone="applied" share={mix.appliedShare} />
                <MixSegment tone="interview" share={mix.interviewShare} />
              </MixTrack>
              <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap", gap: 1 }}>
                <Chip size="small" label={`Open ${formatCount(mix.open)}`} />
                <Chip size="small" color="primary" label={`Applied ${formatCount(mix.applied)}`} />
                <Chip size="small" color="secondary" label={`Interview ${formatCount(mix.interviews)}`} />
              </Stack>
              <Stack spacing={1}>
                <Stack direction="row" sx={{ justifyContent: "space-between" }}>
                  <Typography variant="body2" color="text.secondary">
                    Status set
                  </Typography>
                  <Typography variant="body2">{formatPercent(mix.progressedShare)}</Typography>
                </Stack>
                <ProgressBar variant="determinate" value={mix.progressedShare * 100} />
              </Stack>
              <Typography variant="body2" color="text.secondary">
                Default apply rate {formatRate(props.financial?.defaults.applyRate ?? 0)} · default bonus rate{" "}
                {formatRate(props.financial?.defaults.bonusRate ?? 0)}. Changing defaults does not rewrite existing
                pipeline row rates.
              </Typography>
            </Stack>
          </Panel>
        </Grid>
        <Grid size={{ xs: 12, lg: 5 }}>
          <Panel>
            <Stack spacing={2}>
              <Stack spacing={0.5}>
                <Typography variant="h6" component="h2">
                  Price by pipeline
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Highest priced profile and source pairs. Open a row to edit that pipeline entry.
                </Typography>
              </Stack>
              {props.financialLoading ? (
                <Typography variant="body2" color="text.secondary">
                  Reading listing status from profile main tabs.
                </Typography>
              ) : prices.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  No pipeline rows to price yet.
                </Typography>
              ) : (
                <Stack spacing={1.5}>
                  {prices.map((row) => (
                    <Stack key={row.entryId} spacing={0.5}>
                      <Stack direction="row" sx={{ justifyContent: "space-between", gap: 1 }}>
                        <Link
                          component={RouterLink}
                          to={appPaths.jobApplicationPipeline(row.entryId)}
                          variant="body2"
                          underline="hover"
                        >
                          {row.label}
                        </Link>
                        <Typography variant="body2">{formatPrice(row.price)}</Typography>
                      </Stack>
                      <PriceTrack>
                        <PriceFill share={row.share} />
                      </PriceTrack>
                    </Stack>
                  ))}
                </Stack>
              )}
            </Stack>
          </Panel>
        </Grid>
      </Grid>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, lg: 5 }}>
          <Panel>
            <Stack spacing={2}>
              <Stack spacing={0.5}>
                <Typography variant="h6" component="h2">
                  What needs attention
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Setup and data gaps that block live sheet counts or price.
                </Typography>
              </Stack>
              <Stack spacing={1.5}>
                {attention.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    Reading Google and sheet status.
                  </Typography>
                ) : null}
                {attention.map((item) => (
                  <Alert
                    key={item.id}
                    severity={item.severity}
                    action={
                      <Button color="inherit" size="small" component={RouterLink} to={item.to}>
                        {item.action}
                      </Button>
                    }
                  >
                    <Typography variant="body2">{item.title}</Typography>
                    <Typography variant="body2">{item.detail}</Typography>
                  </Alert>
                ))}
              </Stack>
            </Stack>
          </Panel>
        </Grid>
        <Grid size={{ xs: 12, lg: 7 }}>
          <Panel>
            <Stack spacing={2}>
              <Stack spacing={0.5}>
                <Typography variant="h6" component="h2">
                  Activity this week
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Pipeline, catalog, financial, and Gmail actions for this account, last 7 days.
                </Typography>
              </Stack>
              <DayChart>
                {days.map((day) => (
                  <DayColumn key={day.key}>
                    <Typography variant="caption">{day.count}</Typography>
                    <DayBar share={day.share} />
                    <Typography variant="caption" color="text.secondary">
                      {day.label}
                    </Typography>
                  </DayColumn>
                ))}
              </DayChart>
              <Stack spacing={1}>
                {props.logsLoading ? (
                  <Typography variant="body2" color="text.secondary">
                    Loading activity…
                  </Typography>
                ) : recent.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    No activity recorded yet. Connect Gmail, edit catalog, or run Operations to start the log.
                  </Typography>
                ) : (
                  recent.map((item) => (
                    <Stack
                      key={item.id}
                      direction="row"
                      spacing={1.5}
                      sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
                    >
                      <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                        <Typography variant="body2">{item.summary}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {formatWhen(item.occurredAt)}
                          {item.detail ? ` · ${item.detail}` : ""}
                        </Typography>
                      </Stack>
                      <Chip size="small" label={categoryLabels[item.category] ?? item.category} />
                    </Stack>
                  ))
                )}
              </Stack>
              <Link component={RouterLink} to={appPaths.jobApplication} variant="body2">
                Open Job Application
              </Link>
            </Stack>
          </Panel>
        </Grid>
      </Grid>

      {prices.length > 0 ? (
        <Panel>
          <Stack spacing={2}>
            <Stack spacing={0.5}>
              <Typography variant="h6" component="h2">
                Pipeline contribution
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Each row is a profile paired with a source location. Counts are from that profile main tab.
              </Typography>
            </Stack>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell align="left">Profile</TableCell>
                  <TableCell align="left">Source</TableCell>
                  <TableCell>Listings</TableCell>
                  <TableCell>Applied</TableCell>
                  <TableCell>Interviews</TableCell>
                  <TableCell>Price</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {(props.financial?.rows ?? []).map((row) => (
                  <TableRow key={row.entryId} hover>
                    <TableCell align="left">
                      <Link component={RouterLink} to={appPaths.jobApplicationPipeline(row.entryId)} underline="hover">
                        {row.profileTitle}
                      </Link>
                    </TableCell>
                    <TableCell align="left">{row.sourceLabel}</TableCell>
                    <TableCell>{formatCount(row.total)}</TableCell>
                    <TableCell>{formatCount(row.applied)}</TableCell>
                    <TableCell>{formatCount(row.interviews)}</TableCell>
                    <TableCell>{formatPrice(row.price)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Stack>
        </Panel>
      ) : null}

      {props.isAdmin ? (
        <Panel>
          <Stack spacing={2}>
            <Stack spacing={0.5}>
              <Typography variant="h6" component="h2">
                Directory
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Flexis users. The last active admin cannot be demoted, deactivated, or deleted.
              </Typography>
            </Stack>
            <Grid container spacing={2}>
              <Grid size={{ xs: 6, md: 3 }}>
                <Typography variant="overline" color="text.secondary">
                  Users
                </Typography>
                <Typography variant="h4">
                  {props.usersLoading ? "…" : formatCount(users.total)}
                </Typography>
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <Typography variant="overline" color="text.secondary">
                  Active
                </Typography>
                <Typography variant="h4">
                  {props.usersLoading ? "…" : formatCount(users.active)}
                </Typography>
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <Typography variant="overline" color="text.secondary">
                  Inactive
                </Typography>
                <Typography variant="h4">
                  {props.usersLoading ? "…" : formatCount(users.inactive)}
                </Typography>
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <Typography variant="overline" color="text.secondary">
                  Admins
                </Typography>
                <Typography variant="h4">
                  {props.usersLoading ? "…" : formatCount(users.admins)}
                </Typography>
              </Grid>
            </Grid>
            <Link component={RouterLink} to={appPaths.settings} variant="body2">
              Open Settings
            </Link>
          </Stack>
        </Panel>
      ) : null}
    </Stack>
  );
}

function StatusCard(props: {
  label: string;
  value: string;
  detail: string;
  tone: "success" | "warning" | "default";
  to: string;
  action: string;
}) {
  return (
    <Panel>
      <Stack spacing={1}>
        <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", gap: 1 }}>
          <Typography variant="overline" color="text.secondary">
            {props.label}
          </Typography>
          <Chip
            size="small"
            color={props.tone === "default" ? "default" : props.tone}
            label={props.tone === "success" ? "Live" : props.tone === "warning" ? "Action" : "Wait"}
          />
        </Stack>
        <Typography variant="h6" component="p">
          {props.value}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {props.detail}
        </Typography>
        <Link component={RouterLink} to={props.to} variant="body2">
          {props.action}
        </Link>
      </Stack>
    </Panel>
  );
}
