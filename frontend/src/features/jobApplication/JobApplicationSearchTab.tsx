import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import FormControl from "@mui/material/FormControl";
import InputLabel from "@mui/material/InputLabel";
import Link from "@mui/material/Link";
import LinearProgress from "@mui/material/LinearProgress";
import MenuItem from "@mui/material/MenuItem";
import Select from "@mui/material/Select";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState, type FormEvent } from "react";
import { userFacingError } from "@/shared/api/errors";
import { getGoogleConnection, googleConnectionQueryKey } from "@/shared/api/google";
import {
  jobFinancialQueryKey,
  jobStatisticsQueryKey,
} from "@/shared/api/financial";
import { reindexJobListingProjections } from "@/shared/api/jobListingProjection";
import {
  getJobSearchMeta,
  jobSearchMetaQueryKey,
  searchJobListings,
} from "@/shared/api/jobSearch";
import { isQueryLoading } from "@/shared/api/queryState";
import type { JobSearchHit, JobSearchRequest } from "@/shared/types/jobSearch";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2.5),
}));

const ScoreBar = styled(LinearProgress)(({ theme }) => ({
  height: 8,
  borderRadius: 999,
  backgroundColor: theme.palette.action.hover,
}));

const RankCell = styled(TableCell)({
  fontVariantNumeric: "tabular-nums",
  fontWeight: 600,
  width: 56,
});

function blankToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function sourceLabel(source: string): string {
  if (source === "search-base") {
    return "Search base";
  }
  if (source === "profile") {
    return "Profile main";
  }
  if (source === "profile-archive") {
    return "Archive";
  }
  if (source.startsWith("profile-archive:")) {
    const tab = source.slice("profile-archive:".length).trim();
    return tab.length > 0 ? `Archive ${tab}` : "Archive";
  }
  return source;
}

function scoreTone(score: number): "success" | "warning" | "error" | "inherit" {
  if (score >= 80) {
    return "success";
  }
  if (score >= 55) {
    return "warning";
  }
  if (score >= 28) {
    return "error";
  }
  return "inherit";
}

function JdPreview({ value }: { value: string }) {
  if (!value.trim()) {
    return (
      <Typography variant="body2" color="text.secondary">
        —
      </Typography>
    );
  }

  return (
    <Typography
      variant="body2"
      sx={{
        maxWidth: 280,
        display: "-webkit-box",
        WebkitLineClamp: 3,
        WebkitBoxOrient: "vertical",
        overflow: "hidden",
      }}
    >
      {value}
    </Typography>
  );
}

export function JobApplicationSearchTab() {
  const queryClient = useQueryClient();
  const connectionQuery = useQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });
  const connected = connectionQuery.data?.connected === true;
  const metaQuery = useQuery({
    queryKey: jobSearchMetaQueryKey,
    queryFn: getJobSearchMeta,
    enabled: connected,
  });
  const [profile, setProfile] = useState("");
  const [companyName, setCompanyName] = useState("");
  const [position, setPosition] = useState("");
  const [link, setLink] = useState("");
  const [jd, setJd] = useState("");
  const [status, setStatus] = useState("");
  const [resultError, setResultError] = useState<string | null>(null);
  const [hits, setHits] = useState<JobSearchHit[] | null>(null);
  const [scanned, setScanned] = useState(0);
  const [reindexNotice, setReindexNotice] = useState<string | null>(null);

  const meta = metaQuery.data;
  const metaLoading = isQueryLoading(metaQuery.data, metaQuery.isPending);

  const searchMutation = useMutation({
    mutationFn: (request: JobSearchRequest) => searchJobListings(request),
    onSuccess: (result) => {
      setHits(result.items);
      setScanned(result.scanned);
      setResultError(null);
    },
    onError: (error) => {
      setResultError(userFacingError(error) ?? "Search failed.");
      setHits(null);
    },
  });

  const reindexMutation = useMutation({
    mutationFn: reindexJobListingProjections,
    onSuccess: async (result) => {
      setReindexNotice(`Reindexed ${result.pulled} spreadsheet${result.pulled === 1 ? "" : "s"} from Drive.`);
      setResultError(null);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: jobSearchMetaQueryKey }),
        queryClient.invalidateQueries({ queryKey: jobFinancialQueryKey }),
        queryClient.invalidateQueries({ queryKey: jobStatisticsQueryKey }),
      ]);
    },
    onError: (error) => {
      setReindexNotice(null);
      setResultError(userFacingError(error) ?? "Reindex failed.");
    },
  });

  const profiles = meta?.profiles ?? [];
  const statuses = meta?.statuses ?? [];

  const hasQuery = useMemo(
    () =>
      [profile, companyName, position, link, jd, status].some((value) => value.trim().length > 0),
    [companyName, jd, link, position, profile, status],
  );

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    searchMutation.mutate({
      profile: blankToNull(profile),
      companyName: blankToNull(companyName),
      position: blankToNull(position),
      link: blankToNull(link),
      jd: blankToNull(jd),
      status: blankToNull(status),
    });
  }

  function handleClear() {
    setProfile("");
    setCompanyName("");
    setPosition("");
    setLink("");
    setJd("");
    setStatus("");
    setHits(null);
    setScanned(0);
    setResultError(null);
    setReindexNotice(null);
  }

  return (
    <Stack spacing={2.5}>
      {!connected ? (
        <Alert severity="info">Connect Gmail on Settings (Job Application) to search listings.</Alert>
      ) : null}
      <Panel>
        <Stack spacing={2} component="form" onSubmit={handleSubmit}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Listing search
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Searches the Postgres listing projection built from every profile main and archive
              sheet plus the shared search-base workbook. Profile and Status are exact filters.
              Company, Position, Link, and JD use weighted fuzzy ranking. Use Reindex after editing
              sheets outside Flexis.
            </Typography>
            {meta?.searchBaseUrl ? (
              <Typography variant="body2" color="text.secondary">
                Search base sheet:{" "}
                <Link href={meta.searchBaseUrl} target="_blank" rel="noopener noreferrer">
                  Open in Google Sheets
                </Link>
              </Typography>
            ) : null}
          </Stack>
          {metaLoading ? <LinearProgress /> : null}
          {reindexNotice ? <Alert severity="success">{reindexNotice}</Alert> : null}
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={2}
            sx={{ flexWrap: "wrap", alignItems: "stretch" }}
          >
            <FormControl sx={{ minWidth: 200, flex: 1 }} size="small">
              <InputLabel id="job-search-profile-label">Profile</InputLabel>
              <Select
                labelId="job-search-profile-label"
                label="Profile"
                value={profile}
                onChange={(event) => setProfile(event.target.value)}
              >
                <MenuItem value="">All profiles</MenuItem>
                {profiles.map((item) => (
                  <MenuItem key={`${item.profileId ?? "name"}:${item.title}`} value={item.title}>
                    {item.title}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <FormControl sx={{ minWidth: 160, flex: 1 }} size="small">
              <InputLabel id="job-search-status-label">Status</InputLabel>
              <Select
                labelId="job-search-status-label"
                label="Status"
                value={status}
                onChange={(event) => setStatus(event.target.value)}
              >
                <MenuItem value="">Any status</MenuItem>
                {statuses.map((item) => (
                  <MenuItem key={item} value={item}>
                    {item}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField
              label="Company Name"
              value={companyName}
              onChange={(event) => setCompanyName(event.target.value)}
              fullWidth
              size="small"
            />
            <TextField
              label="Position"
              value={position}
              onChange={(event) => setPosition(event.target.value)}
              fullWidth
              size="small"
            />
          </Stack>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField
              label="Link"
              value={link}
              onChange={(event) => setLink(event.target.value)}
              fullWidth
              size="small"
            />
            <TextField
              label="JD"
              value={jd}
              onChange={(event) => setJd(event.target.value)}
              fullWidth
              size="small"
              multiline
              minRows={2}
            />
          </Stack>
          {resultError ? <Alert severity="error">{resultError}</Alert> : null}
          <Stack direction="row" spacing={1.5} sx={{ justifyContent: "flex-end" }}>
            <Button
              type="button"
              variant="text"
              onClick={() => reindexMutation.mutate()}
              disabled={!connected || reindexMutation.isPending || searchMutation.isPending}
              loading={reindexMutation.isPending}
            >
              Reindex from Sheets
            </Button>
            <Button type="button" variant="text" onClick={handleClear} disabled={searchMutation.isPending}>
              Clear
            </Button>
            <Button
              type="submit"
              disabled={!connected || searchMutation.isPending}
              loading={searchMutation.isPending}
            >
              {hasQuery ? "Search" : "Browse all"}
            </Button>
          </Stack>
        </Stack>
      </Panel>

      {hits ? (
        <Panel>
          <Stack spacing={2}>
            <Stack
              direction={{ xs: "column", sm: "row" }}
              spacing={1}
              sx={{ alignItems: { sm: "center" }, justifyContent: "space-between" }}
            >
              <Typography variant="h6" component="h2">
                Results
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {hits.length} match{hits.length === 1 ? "" : "es"} · scanned {scanned} listing
                {scanned === 1 ? "" : "s"}
              </Typography>
            </Stack>
            {hits.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No listings matched. Broaden text fields or clear Profile / Status filters.
              </Typography>
            ) : (
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell align="left">Rank</TableCell>
                      <TableCell align="left">Similarity</TableCell>
                      <TableCell align="left">Profile</TableCell>
                      <TableCell align="left">Company</TableCell>
                      <TableCell align="left">Position</TableCell>
                      <TableCell align="left">Status</TableCell>
                      <TableCell align="left">Source</TableCell>
                      <TableCell align="left">Link</TableCell>
                      <TableCell align="left">JD</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {hits.map((hit) => (
                      <TableRow key={`${hit.source}:${hit.rank}:${hit.profile}:${hit.link}:${hit.companyName}`}>
                        <RankCell align="left">#{hit.rank}</RankCell>
                        <TableCell align="left" sx={{ minWidth: 140 }}>
                          <Stack spacing={0.75}>
                            <Typography
                              variant="body2"
                              color={`${scoreTone(hit.score)}.main`}
                              sx={{ fontVariantNumeric: "tabular-nums", fontWeight: 600 }}
                            >
                              {hit.score.toFixed(1)}%
                            </Typography>
                            <ScoreBar
                              variant="determinate"
                              value={Math.min(100, Math.max(0, hit.score))}
                              color={scoreTone(hit.score) === "inherit" ? "primary" : scoreTone(hit.score)}
                            />
                          </Stack>
                        </TableCell>
                        <TableCell align="left">{hit.profile || "—"}</TableCell>
                        <TableCell align="left">{hit.companyName || "—"}</TableCell>
                        <TableCell align="left">{hit.position || "—"}</TableCell>
                        <TableCell align="left">
                          {hit.status ? <Chip size="small" label={hit.status} /> : "—"}
                        </TableCell>
                        <TableCell align="left">
                          <Chip size="small" variant="outlined" label={sourceLabel(hit.source)} />
                        </TableCell>
                        <TableCell align="left">
                          {hit.link ? (
                            <Link href={hit.link} target="_blank" rel="noopener noreferrer">
                              Open
                            </Link>
                          ) : (
                            "—"
                          )}
                        </TableCell>
                        <TableCell align="left">
                          <JdPreview value={hit.jd} />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Stack>
        </Panel>
      ) : null}
    </Stack>
  );
}
