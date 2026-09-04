import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { formatCount, formatPrice } from "@/features/jobApplication/financialUi";
import type { StatisticsPeriodRow } from "@/features/jobApplication/statisticsPeriod";
import type { JobStatisticsProfile } from "@/shared/types/jobApplication";

export function StatisticsTodayTable({ profiles }: { profiles: JobStatisticsProfile[] }) {
  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell align="left">Profile</TableCell>
            <TableCell align="left">Ready</TableCell>
            <TableCell align="left">Not ready</TableCell>
            <TableCell align="left">Applied</TableCell>
            <TableCell align="left">Interviews</TableCell>
            <TableCell align="left">Unapplied</TableCell>
            <TableCell align="left">Listings</TableCell>
            <TableCell align="left">Price</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {profiles.length === 0 ? (
            <TableRow>
              <TableCell colSpan={8} align="left">
                <Typography variant="body2" color="text.secondary">
                  Add pipeline rows on Operations and run Update to track today.
                </Typography>
              </TableCell>
            </TableRow>
          ) : (
            profiles.map((profile) => (
              <TableRow key={profile.profileId} hover>
                <TableCell align="left">{profile.profileTitle}</TableCell>
                <TableCell align="left">{formatCount(profile.todayReady)}</TableCell>
                <TableCell align="left">{formatCount(profile.todayNotReady)}</TableCell>
                <TableCell align="left">{formatCount(profile.todayApplied)}</TableCell>
                <TableCell align="left">{formatCount(profile.todayInterviews)}</TableCell>
                <TableCell align="left">{formatCount(profile.todayUnapplied)}</TableCell>
                <TableCell align="left">{formatCount(profile.todayTotal)}</TableCell>
                <TableCell align="left">{formatPrice(profile.todayPrice)}</TableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

export function StatisticsPeriodTable({ rows }: { rows: StatisticsPeriodRow[] }) {
  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell align="left">Period</TableCell>
            <TableCell align="left">Applied</TableCell>
            <TableCell align="left">Interviews</TableCell>
            <TableCell align="left">Unapplied</TableCell>
            <TableCell align="left">Listings</TableCell>
            <TableCell align="left">Price</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.length === 0 ? (
            <TableRow>
              <TableCell colSpan={6} align="left">
                <Typography variant="body2" color="text.secondary">
                  No captured points in this range yet.
                </Typography>
              </TableCell>
            </TableRow>
          ) : (
            [...rows].reverse().map((row) => (
              <TableRow key={row.key} hover>
                <TableCell align="left">{row.label}</TableCell>
                <TableCell align="left">{formatCount(row.applied)}</TableCell>
                <TableCell align="left">{formatCount(row.interviews)}</TableCell>
                <TableCell align="left">{formatCount(row.unapplied)}</TableCell>
                <TableCell align="left">{formatCount(row.total)}</TableCell>
                <TableCell align="left">{formatPrice(row.price)}</TableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
