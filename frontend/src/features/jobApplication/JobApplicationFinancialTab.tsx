import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
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
import { useEffect, useMemo, useState } from "react";
import { isQueryLoading } from "@/shared/api/queryState";
import { getJobFinancialBoard, jobFinancialQueryKey, updateJobFinancialRates } from "@/shared/api/financial";
import {
  FinancialMetricCell,
  FinancialSummaryCards,
} from "@/features/jobApplication/FinancialSummaryCards";
import { FinancialPerformanceChart } from "@/features/jobApplication/FinancialPerformanceChart";
import { formatFinancialMetrics, formatPrice, formatRate, parseRate } from "@/features/jobApplication/financialUi";
import { refreshJobApplicationWorkspace } from "@/features/jobApplication/refreshWorkspace";
import type { JobFinancialRow } from "@/shared/types/jobApplication";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  overflow: "hidden",
}));

const SelectionCard = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2),
}));

const ScrollTable = styled(TableContainer)({
  maxHeight: 560,
});

const RateField = styled(TextField)({
  width: 108,
});

const GroupHeader = styled(TableCell)(({ theme }) => ({
  backgroundColor: theme.palette.action.hover,
  fontWeight: 700,
}));

function sumRows(rows: JobFinancialRow[], pick: (row: JobFinancialRow) => number): number {
  return rows.reduce((sum, row) => {
    const value = pick(row);
    return sum + (typeof value === "number" && Number.isFinite(value) ? value : 0);
  }, 0);
}

export function JobApplicationFinancialTab() {
  const queryClient = useQueryClient();
  const boardQuery = useQuery({
    queryKey: jobFinancialQueryKey,
    queryFn: getJobFinancialBoard,
  });
  const [selected, setSelected] = useState<string[]>([]);

  const rows = boardQuery.data?.rows ?? [];
  const boardLoading = isQueryLoading(boardQuery.data, boardQuery.isPending);
  const selectedSet = useMemo(() => new Set(selected.filter((id) => rows.some((row) => row.entryId === id))), [rows, selected]);
  const selectedRows = rows.filter((row) => selectedSet.has(row.entryId));
  const allSelected = rows.length > 0 && selectedRows.length === rows.length;
  const someSelected = selectedRows.length > 0 && !allSelected;

  const ratesMutation = useMutation({
    mutationFn: ({ entryId, applyRate, bonusRate }: { entryId: string; applyRate: number; bonusRate: number }) =>
      updateJobFinancialRates(entryId, { applyRate, bonusRate }),
    onSuccess: async () => {
      await refreshJobApplicationWorkspace(queryClient);
    },
  });

  function toggleAll() {
    setSelected(allSelected ? [] : rows.map((row) => row.entryId));
  }

  function toggleRow(entryId: string) {
    setSelected((current) =>
      current.includes(entryId) ? current.filter((id) => id !== entryId) : [...current, entryId],
    );
  }

  return (
    <Stack spacing={2}>
      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: "flex-start", justifyContent: "space-between" }}
      >
        <Stack spacing={0.5}>
          <Typography variant="h6" component="h2">
            Financial
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Today is listings from the last Update. Ready is Download filled. Not ready is blank
            Download and blank Status. Applied and blank Status count among ready rows only. Main is
            the full profile main sheet. Archived uses numbered sheets from Forward. Lifetime is main
            plus archived.
          </Typography>
        </Stack>
        <Button variant="text" onClick={() => void boardQuery.refetch()} disabled={boardQuery.isFetching}>
          Refresh
        </Button>
      </Stack>

      <FinancialSummaryCards
        loading={boardLoading}
        todayPrice={boardQuery.data?.todayAllPrice ?? 0}
        todayTotal={boardQuery.data?.todayAllTotal ?? 0}
        todayReady={boardQuery.data?.todayAllReady ?? 0}
        todayNotReady={boardQuery.data?.todayAllNotReady ?? 0}
        todayApplied={boardQuery.data?.todayAllApplied ?? 0}
        todayInterviews={boardQuery.data?.todayAllInterviews ?? 0}
        mainPrice={boardQuery.data?.allPrice ?? 0}
        mainTotal={boardQuery.data?.allTotal ?? 0}
        mainReady={boardQuery.data?.allReady ?? 0}
        mainNotReady={boardQuery.data?.allNotReady ?? 0}
        mainApplied={boardQuery.data?.allApplied ?? 0}
        mainInterviews={boardQuery.data?.allInterviews ?? 0}
        archivedPrice={boardQuery.data?.archivedAllPrice ?? 0}
        archivedTotal={boardQuery.data?.archivedAllTotal ?? 0}
        archivedReady={boardQuery.data?.archivedAllReady ?? 0}
        archivedNotReady={boardQuery.data?.archivedAllNotReady ?? 0}
        archivedApplied={boardQuery.data?.archivedAllApplied ?? 0}
        archivedInterviews={boardQuery.data?.archivedAllInterviews ?? 0}
        lifetimePrice={boardQuery.data?.lifetimeAllPrice ?? 0}
        lifetimeTotal={boardQuery.data?.lifetimeAllTotal ?? 0}
        lifetimeReady={boardQuery.data?.lifetimeAllReady ?? 0}
        lifetimeNotReady={boardQuery.data?.lifetimeAllNotReady ?? 0}
        lifetimeApplied={boardQuery.data?.lifetimeAllApplied ?? 0}
        lifetimeInterviews={boardQuery.data?.lifetimeAllInterviews ?? 0}
      />

      <FinancialPerformanceChart
        loading={boardLoading}
        history={boardQuery.data?.history ?? []}
      />

      <SelectionCard>
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={2}
          sx={{ alignItems: { md: "center" }, justifyContent: "space-between" }}
        >
          <Stack spacing={0.25}>
            <Typography variant="subtitle2">Selected rows</Typography>
            <Typography variant="body2" color="text.secondary">
              {selectedRows.length === 0
                ? "Select rows to price a subset."
                : `${selectedRows.length} selected`}
            </Typography>
          </Stack>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ minWidth: 0 }}>
            <Stack spacing={0.25}>
              <Typography variant="caption" color="text.secondary">
                Today
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 600 }}>
                {selectedRows.length === 0
                  ? "—"
                  : formatPrice(sumRows(selectedRows, (row) => row.todayPrice))}
              </Typography>
              {selectedRows.length > 0 ? (
                <Typography variant="caption" color="text.secondary">
                  {formatFinancialMetrics(
                    sumRows(selectedRows, (row) => row.todayApplied),
                    sumRows(selectedRows, (row) => row.todayInterviews),
                    sumRows(selectedRows, (row) => row.todayTotal),
                    sumRows(selectedRows, (row) => row.todayReady),
                    sumRows(selectedRows, (row) => row.todayNotReady),
                  )}
                </Typography>
              ) : null}
            </Stack>
            <Stack spacing={0.25}>
              <Typography variant="caption" color="text.secondary">
                Main
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 600 }}>
                {selectedRows.length === 0 ? "—" : formatPrice(sumRows(selectedRows, (row) => row.price))}
              </Typography>
              {selectedRows.length > 0 ? (
                <Typography variant="caption" color="text.secondary">
                  {formatFinancialMetrics(
                    sumRows(selectedRows, (row) => row.applied),
                    sumRows(selectedRows, (row) => row.interviews),
                    sumRows(selectedRows, (row) => row.total),
                    sumRows(selectedRows, (row) => row.ready),
                    sumRows(selectedRows, (row) => row.notReady),
                  )}
                </Typography>
              ) : null}
            </Stack>
            <Stack spacing={0.25}>
              <Typography variant="caption" color="text.secondary">
                Archived
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 600 }}>
                {selectedRows.length === 0
                  ? "—"
                  : formatPrice(sumRows(selectedRows, (row) => row.archivedPrice))}
              </Typography>
            </Stack>
            <Stack spacing={0.25}>
              <Typography variant="caption" color="text.secondary">
                Lifetime
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 700 }}>
                {selectedRows.length === 0
                  ? "—"
                  : formatPrice(sumRows(selectedRows, (row) => row.lifetimePrice))}
              </Typography>
            </Stack>
          </Stack>
        </Stack>
      </SelectionCard>

      <Panel>
        <ScrollTable>
          <Table stickyHeader size="small">
            <TableHead>
              <TableRow>
                <TableCell padding="checkbox">
                  <Checkbox
                    checked={allSelected}
                    indeterminate={someSelected}
                    disabled={rows.length === 0}
                    onChange={toggleAll}
                    slotProps={{ input: { "aria-label": "Select all pipeline rows" } }}
                  />
                </TableCell>
                <TableCell>Profile</TableCell>
                <TableCell>Source</TableCell>
                <GroupHeader>Today</GroupHeader>
                <GroupHeader>Main</GroupHeader>
                <GroupHeader>Archived</GroupHeader>
                <GroupHeader>Lifetime</GroupHeader>
                <GroupHeader>Apply rate</GroupHeader>
                <GroupHeader>Bonus rate</GroupHeader>
              </TableRow>
            </TableHead>
            <TableBody>
              {boardLoading ? (
                <TableRow>
                  <TableCell colSpan={9}>
                    <Typography variant="body2" color="text.secondary">
                      Loading…
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : rows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={9}>
                    <Typography variant="body2" color="text.secondary">
                      Add pipeline rows on Operations to price listings.
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                rows.map((row) => (
                  <TableRow key={row.entryId} hover selected={selectedSet.has(row.entryId)}>
                    <TableCell padding="checkbox">
                      <Checkbox
                        checked={selectedSet.has(row.entryId)}
                        onChange={() => toggleRow(row.entryId)}
                        slotProps={{ input: { "aria-label": `Select ${row.profileTitle}` } }}
                      />
                    </TableCell>
                    <TableCell>{row.profileTitle}</TableCell>
                    <TableCell>{row.sourceLabel}</TableCell>
                    <TableCell>
                      <FinancialMetricCell
                        applied={row.todayApplied}
                        interviews={row.todayInterviews}
                        price={row.todayPrice}
                      />
                    </TableCell>
                    <TableCell>
                      <FinancialMetricCell
                        applied={row.applied}
                        interviews={row.interviews}
                        price={row.price}
                      />
                    </TableCell>
                    <TableCell>
                      <FinancialMetricCell
                        applied={row.archivedApplied}
                        interviews={row.archivedInterviews}
                        price={row.archivedPrice}
                      />
                    </TableCell>
                    <TableCell>
                      <FinancialMetricCell
                        applied={row.lifetimeApplied}
                        interviews={row.lifetimeInterviews}
                        price={row.lifetimePrice}
                        emphasize
                      />
                    </TableCell>
                    <TableCell>
                      <RateCell
                        value={row.applyRate}
                        disabled={ratesMutation.isPending}
                        ariaLabel={`Apply rate for ${row.profileTitle}`}
                        onCommit={(applyRate) =>
                          ratesMutation.mutate({
                            entryId: row.entryId,
                            applyRate,
                            bonusRate: row.bonusRate,
                          })
                        }
                      />
                    </TableCell>
                    <TableCell>
                      <RateCell
                        value={row.bonusRate}
                        disabled={ratesMutation.isPending}
                        ariaLabel={`Bonus rate for ${row.profileTitle}`}
                        onCommit={(bonusRate) =>
                          ratesMutation.mutate({
                            entryId: row.entryId,
                            applyRate: row.applyRate,
                            bonusRate,
                          })
                        }
                      />
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </ScrollTable>
      </Panel>
    </Stack>
  );
}

function RateCell({
  value,
  disabled,
  ariaLabel,
  onCommit,
}: {
  value: number;
  disabled: boolean;
  ariaLabel: string;
  onCommit: (next: number) => void;
}) {
  const [text, setText] = useState(() => formatRate(value));

  useEffect(() => {
    setText(formatRate(value));
  }, [value]);

  return (
    <RateField
      size="small"
      value={text}
      disabled={disabled}
      onChange={(event) => setText(event.target.value)}
      onBlur={() => {
        const parsed = parseRate(text);
        if (parsed === null) {
          setText(formatRate(value));
          return;
        }

        setText(formatRate(parsed));
        if (parsed !== Number(value.toFixed(4))) {
          onCommit(parsed);
        }
      }}
      slotProps={{
        htmlInput: {
          "aria-label": ariaLabel,
          inputMode: "decimal",
          step: "0.0001",
          min: "0",
        },
      }}
    />
  );
}
