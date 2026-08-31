import Box from "@mui/material/Box";
import { styled } from "@mui/material/styles";

export const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3.5),
}));

export const EmptyState = styled(Box)(({ theme }) => ({
  border: `1px dashed ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(6, 3),
  textAlign: "center",
}));
