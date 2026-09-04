import Box from "@mui/material/Box";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { formatRate } from "@/features/jobApplication/financialUi";
import { getJobFinancialBoard, jobFinancialQueryKey } from "@/shared/api/financial";
import { isQueryLoading } from "@/shared/api/queryState";
import { appPaths } from "@/shared/config/paths";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  overflow: "hidden",
}));

const ScrollTable = styled(TableContainer)({
  maxHeight: 640,
});

const ClickRow = styled(TableRow)(({ theme }) => ({
  cursor: "pointer",
  "&:hover": {
    backgroundColor: theme.palette.action.hover,
  },
}));

export function JobApplicationProfilesTab() {
  const navigate = useNavigate();
  const boardQuery = useQuery({
    queryKey: jobFinancialQueryKey,
    queryFn: getJobFinancialBoard,
  });
  const rows = boardQuery.data?.rows ?? [];
  const boardLoading = isQueryLoading(boardQuery.data, boardQuery.isPending);

  return (
    <Stack spacing={2}>
      <Stack spacing={0.5}>
        <Typography variant="h6" component="h2">
          Profiles
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Each row is a profile paired with a source location. Open a row for rates, apply status,
          profile info, and related details.
        </Typography>
      </Stack>
      <Panel>
        <ScrollTable>
          <Table stickyHeader size="small">
            <TableHead>
              <TableRow>
                <TableCell align="left">Profile</TableCell>
                <TableCell align="left">Source</TableCell>
                <TableCell align="left">Link</TableCell>
                <TableCell align="left">Apply rate</TableCell>
                <TableCell align="left">Bonus rate</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {boardLoading ? (
                <TableRow>
                  <TableCell colSpan={5} align="left">
                    <Typography variant="body2" color="text.secondary">
                      Loading…
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : rows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} align="left">
                    <Typography variant="body2" color="text.secondary">
                      Add pipeline rows on Operations to see profiles here.
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                rows.map((row) => (
                  <ClickRow
                    key={row.entryId}
                    hover
                    onClick={() => navigate(appPaths.jobApplicationProfile(row.entryId))}
                  >
                    <TableCell align="left">{row.profileTitle}</TableCell>
                    <TableCell align="left">{row.sourceLabel}</TableCell>
                    <TableCell align="left" sx={{ maxWidth: 280 }}>
                      {row.profileUrl ? (
                        <Link
                          href={row.profileUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          onClick={(event) => event.stopPropagation()}
                          sx={{ wordBreak: "break-all" }}
                        >
                          {row.profileUrl}
                        </Link>
                      ) : (
                        <Typography variant="body2" color="text.secondary">
                          —
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell align="left">{formatRate(row.applyRate)}</TableCell>
                    <TableCell align="left">{formatRate(row.bonusRate)}</TableCell>
                  </ClickRow>
                ))
              )}
            </TableBody>
          </Table>
        </ScrollTable>
      </Panel>
    </Stack>
  );
}
