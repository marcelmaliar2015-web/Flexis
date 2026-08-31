import Alert from "@mui/material/Alert";
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
import { formatCount, formatPrice, formatRate, parseRate } from "@/features/jobApplication/financialUi";
import { refreshJobApplicationWorkspace } from "@/features/jobApplication/refreshWorkspace";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  overflow: "hidden",
}));

const SummaryCard = styled(Box)(({ theme }) => ({
  flex: 1,
  minWidth: 0,
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2.5),
}));

const ScrollTable = styled(TableContainer)({
  maxHeight: 560,
});

const RateField = styled(TextField)({
  width: 108,
});

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
  const selectedPrice = selectedRows.reduce((sum, row) => sum + row.price, 0);
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
            Counts come from each profile main tab. Price is applied times apply rate plus interviews times bonus
            rate.
          </Typography>
        </Stack>
        <Button variant="text" onClick={() => void boardQuery.refetch()} disabled={boardQuery.isFetching}>
          Refresh
        </Button>
      </Stack>
      <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
        <SummaryCard>
          <Typography variant="overline" color="text.secondary">
            All sheets
          </Typography>
          <Typography variant="h4">
            {boardLoading ? "…" : formatPrice(boardQuery.data?.allPrice ?? 0)}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {boardLoading
              ? "Reading profile main tabs."
              : `${formatCount(boardQuery.data?.allTotal ?? 0)} listings · ${formatCount(boardQuery.data?.allApplied ?? 0)} applied · ${formatCount(boardQuery.data?.allInterviews ?? 0)} interviews`}
          </Typography>
        </SummaryCard>
        <SummaryCard>
          <Typography variant="overline" color="text.secondary">
            Selected rows
          </Typography>
          <Typography variant="h4">{selectedRows.length === 0 ? "—" : formatPrice(selectedPrice)}</Typography>
          <Typography variant="body2" color="text.secondary">
            {selectedRows.length === 0
              ? "Select rows to price a subset."
              : `${formatCount(selectedRows.length)} selected · ${formatCount(selectedRows.reduce((sum, row) => sum + row.total, 0))} listings · ${formatCount(selectedRows.reduce((sum, row) => sum + row.applied, 0))} applied · ${formatCount(selectedRows.reduce((sum, row) => sum + row.interviews, 0))} interviews`}
          </Typography>
        </SummaryCard>
      </Stack>
      <Panel>
        <ScrollTable>
          <Table stickyHeader>
            <TableHead>
              <TableRow>
                <TableCell padding="checkbox">
                  <Checkbox
                    checked={allSelected}
                    indeterminate={someSelected}
                    disabled={rows.length === 0}
                    onChange={toggleAll}
                    inputProps={{ "aria-label": "Select all pipeline rows" }}
                  />
                </TableCell>
                <TableCell>Profile</TableCell>
                <TableCell>Source</TableCell>
                <TableCell>Total</TableCell>
                <TableCell>Applied</TableCell>
                <TableCell>Interviews</TableCell>
                <TableCell>Apply Rate</TableCell>
                <TableCell>Bonus Rate</TableCell>
                <TableCell>Price</TableCell>
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
                        inputProps={{ "aria-label": `Select ${row.profileTitle}` }}
                      />
                    </TableCell>
                    <TableCell>{row.profileTitle}</TableCell>
                    <TableCell>{row.sourceLabel}</TableCell>
                    <TableCell>{formatCount(row.total)}</TableCell>
                    <TableCell>{formatCount(row.applied)}</TableCell>
                    <TableCell>{formatCount(row.interviews)}</TableCell>
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
                    <TableCell>{formatPrice(row.price)}</TableCell>
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
