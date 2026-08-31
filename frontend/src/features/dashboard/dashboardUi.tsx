import Box from "@mui/material/Box";
import LinearProgress from "@mui/material/LinearProgress";
import { styled } from "@mui/material/styles";

export const AccentRule = styled("span")(({ theme }) => ({
  display: "block",
  width: 40,
  height: 2,
  marginTop: theme.spacing(1.5),
  backgroundColor: theme.palette.secondary.main,
}));

export const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2.5),
  height: "100%",
  boxSizing: "border-box",
}));

export const KpiCard = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2.5),
  height: "100%",
  boxSizing: "border-box",
  minWidth: 0,
}));

export const MixTrack = styled("div")(({ theme }) => ({
  display: "flex",
  width: "100%",
  height: 14,
  overflow: "hidden",
  borderRadius: 999,
  backgroundColor: theme.palette.action.hover,
}));

export const MixSegment = styled("span", {
  shouldForwardProp: (prop) => prop !== "tone" && prop !== "share",
})<{ tone: "open" | "applied" | "interview"; share: number }>(({ theme, tone, share }) => ({
  width: `${Math.max(share, 0) * 100}%`,
  minWidth: share > 0 ? 2 : 0,
  backgroundColor:
    tone === "applied"
      ? theme.palette.primary.main
      : tone === "interview"
        ? theme.palette.secondary.main
        : theme.palette.primary.light,
  opacity: tone === "open" ? 0.35 : 1,
}));

export const PriceTrack = styled("div")(({ theme }) => ({
  height: 8,
  overflow: "hidden",
  borderRadius: 999,
  backgroundColor: theme.palette.action.hover,
}));

export const PriceFill = styled("span", {
  shouldForwardProp: (prop) => prop !== "share",
})<{ share: number }>(({ theme, share }) => ({
  display: "block",
  height: "100%",
  width: `${Math.max(share, 0) * 100}%`,
  borderRadius: 999,
  backgroundColor: theme.palette.primary.main,
}));

export const DayChart = styled("div")(({ theme }) => ({
  display: "flex",
  alignItems: "flex-end",
  gap: theme.spacing(1),
  height: 120,
}));

export const DayColumn = styled("div")({
  flex: 1,
  minWidth: 0,
  height: "100%",
  display: "flex",
  flexDirection: "column",
  alignItems: "center",
  justifyContent: "flex-end",
  gap: 6,
});

export const DayBar = styled("span", {
  shouldForwardProp: (prop) => prop !== "share",
})<{ share: number }>(({ theme, share }) => ({
  display: "block",
  width: "100%",
  maxWidth: 28,
  height: `${Math.max(share, 0.04) * 100}%`,
  borderRadius: "6px 6px 2px 2px",
  backgroundColor: share > 0 ? theme.palette.secondary.main : theme.palette.action.hover,
}));

export const ProgressBar = styled(LinearProgress)(({ theme }) => ({
  height: 8,
  borderRadius: 999,
  backgroundColor: theme.palette.action.hover,
}));
