import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { jobApplicationLogsQueryKey, listJobApplicationLogs } from "@/shared/api/jobApplicationLogs";
import type { JobApplicationLog } from "@/shared/types/jobApplication";

const FilterRow = styled(Stack)(({ theme }) => ({
  flexWrap: "wrap",
  gap: theme.spacing(1),
}));

const DayHeading = styled(Typography)(({ theme }) => ({
  paddingTop: theme.spacing(1),
}));

const LogCard = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2, 2.5),
}));

const EmptyState = styled(Box)(({ theme }) => ({
  border: `1px dashed ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(6, 3),
  textAlign: "center",
}));

type LogCategoryFilter = "all" | "pipeline" | "catalog" | "financial" | "account";

const categoryFilters: { value: LogCategoryFilter; label: string }[] = [
  { value: "all", label: "All" },
  { value: "pipeline", label: "Pipeline" },
  { value: "catalog", label: "Catalog" },
  { value: "financial", label: "Financial" },
  { value: "account", label: "Account" },
];

const actionLabels: Record<string, string> = {
  create: "Created",
  update: "Edited",
  delete: "Deleted",
  "delete-all": "Deleted all",
  "update-listings": "Updated listings",
  "update-all": "Update all",
  forward: "Forwarded",
  "forward-all": "Forward all",
  "ban-add": "Banned company",
  "ban-edit": "Edited ban",
  "ban-remove": "Removed ban",
  "create-profile": "Profile created",
  "rename-profile": "Profile renamed",
  "delete-profile": "Profile deleted",
  "create-source": "Source created",
  "rename-source": "Source renamed",
  "delete-source": "Source deleted",
  "add-location": "Location added",
  "rename-location": "Location renamed",
  "delete-location": "Location deleted",
  defaults: "Default rates",
  rates: "Row rates",
  "gmail-connect": "Gmail connected",
  "gmail-disconnect": "Gmail disconnected",
};

const categoryLabels: Record<string, string> = {
  pipeline: "Pipeline",
  catalog: "Catalog",
  financial: "Financial",
  account: "Account",
};

function categoryColor(category: string): "primary" | "secondary" | "success" | "default" {
  if (category === "pipeline") {
    return "primary";
  }
  if (category === "financial") {
    return "secondary";
  }
  if (category === "account") {
    return "success";
  }
  return "default";
}

function formatDay(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    weekday: "long",
    month: "long",
    day: "numeric",
    year: "numeric",
  }).format(new Date(value));
}

function formatTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    hour: "numeric",
    minute: "2-digit",
    second: "2-digit",
  }).format(new Date(value));
}

function groupByDay(items: JobApplicationLog[]): { day: string; items: JobApplicationLog[] }[] {
  const groups = new Map<string, JobApplicationLog[]>();
  for (const item of items) {
    const day = formatDay(item.occurredAt);
    const current = groups.get(day);
    if (current) {
      current.push(item);
      continue;
    }

    groups.set(day, [item]);
  }

  return [...groups.entries()].map(([day, grouped]) => ({ day, items: grouped }));
}

export function JobApplicationLogsTab() {
  const logsQuery = useQuery({
    queryKey: jobApplicationLogsQueryKey,
    queryFn: listJobApplicationLogs,
  });
  const [category, setCategory] = useState<LogCategoryFilter>("all");
  const [query, setQuery] = useState("");

  const filtered = useMemo(() => {
    const items = logsQuery.data ?? [];
    const needle = query.trim().toLowerCase();
    return items.filter((item) => {
      if (category !== "all" && item.category !== category) {
        return false;
      }

      if (needle.length === 0) {
        return true;
      }

      return `${item.summary} ${item.detail} ${item.action} ${item.category}`.toLowerCase().includes(needle);
    });
  }, [category, logsQuery.data, query]);

  const groups = useMemo(() => groupByDay(filtered), [filtered]);

  return (
    <Stack spacing={2}>
      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
      >
        <Stack spacing={0.5}>
          <Typography variant="h6" component="h2">
            Activity log
          </Typography>
          <Typography variant="body2" color="text.secondary">
            A dated record of pipeline, catalog, financial, and Gmail actions for this account. Newest events appear
            first.
          </Typography>
        </Stack>
        <Button variant="text" onClick={() => void logsQuery.refetch()} disabled={logsQuery.isFetching}>
          Refresh
        </Button>
      </Stack>
      <TextField
        fullWidth
        size="small"
        label="Search activity"
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        placeholder="Search summaries, details, or actions"
      />
      <FilterRow direction="row">
        {categoryFilters.map((filter) => (
          <Chip
            key={filter.value}
            label={filter.label}
            color={category === filter.value ? "primary" : "default"}
            variant={category === filter.value ? "filled" : "outlined"}
            onClick={() => setCategory(filter.value)}
          />
        ))}
      </FilterRow>
      {logsQuery.isSuccess && (logsQuery.data?.length ?? 0) === 0 ? (
        <EmptyState>
          <Typography variant="subtitle1">No activity recorded yet</Typography>
          <Typography variant="body2" color="text.secondary">
            Pipeline updates, catalog changes, rate edits, and Gmail connect or disconnect will appear here with a
            timestamp and a detailed description.
          </Typography>
        </EmptyState>
      ) : null}
      {logsQuery.isSuccess && (logsQuery.data?.length ?? 0) > 0 && filtered.length === 0 ? (
        <EmptyState>
          <Typography variant="subtitle1">No matching activity</Typography>
          <Typography variant="body2" color="text.secondary">
            Try another category or a broader search.
          </Typography>
        </EmptyState>
      ) : null}
      {groups.map((group) => (
        <Stack key={group.day} spacing={1.5}>
          <DayHeading variant="overline" color="secondary">
            {group.day}
          </DayHeading>
          {group.items.map((item) => (
            <LogCard key={item.id}>
              <Stack spacing={1}>
                <Stack
                  direction="row"
                  spacing={1}
                  useFlexGap
                  sx={{ alignItems: "center", justifyContent: "space-between", flexWrap: "wrap" }}
                >
                  <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: "center", flexWrap: "wrap" }}>
                    <Chip size="small" label={actionLabels[item.action] ?? item.action} color={categoryColor(item.category)} />
                    <Chip
                      size="small"
                      variant="outlined"
                      label={categoryLabels[item.category] ?? item.category}
                    />
                  </Stack>
                  <Typography variant="caption" color="text.secondary">
                    {formatTime(item.occurredAt)}
                  </Typography>
                </Stack>
                <Typography variant="subtitle1">{item.summary}</Typography>
                {item.detail ? (
                  <Typography variant="body2" color="text.secondary">
                    {item.detail}
                  </Typography>
                ) : null}
              </Stack>
            </LogCard>
          ))}
        </Stack>
      ))}
    </Stack>
  );
}
