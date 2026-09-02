import Chip from "@mui/material/Chip";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { mailboxMessageUrl, providerLabel } from "@/features/mailCheck/mailCheckUi";
import type { MailCheckInboxItem } from "@/shared/types/mailCheck";

type MailCheckInboxTableProps = {
  items: MailCheckInboxItem[];
};

export function MailCheckInboxTable({ items }: MailCheckInboxTableProps) {
  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell align="left">Mailbox</TableCell>
            <TableCell align="left">From</TableCell>
            <TableCell align="left">Subject</TableCell>
            <TableCell align="left">Label</TableCell>
            <TableCell align="left">Date</TableCell>
            <TableCell align="left">Open</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {items.map((item) => (
            <TableRow key={`${item.mailboxId}-${item.id}`}>
              <TableCell align="left">
                <Stack spacing={0.25}>
                  <Typography variant="body2">{providerLabel(item.mailboxProvider)}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {item.mailboxEmail}
                  </Typography>
                </Stack>
              </TableCell>
              <TableCell align="left">{item.from}</TableCell>
              <TableCell align="left">
                <Stack spacing={0.5}>
                  <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                    <Typography variant="body2">{item.subject || "(no subject)"}</Typography>
                    {item.starred ? <Chip size="small" label="Pinned" /> : null}
                  </Stack>
                  {item.snippet ? (
                    <Typography variant="caption" color="text.secondary">
                      {item.snippet}
                    </Typography>
                  ) : null}
                </Stack>
              </TableCell>
              <TableCell align="left">{item.label}</TableCell>
              <TableCell align="left">{item.date}</TableCell>
              <TableCell align="left">
                <Link
                  href={mailboxMessageUrl(item.mailboxProvider, item.threadId, item.id)}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  Open
                </Link>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
