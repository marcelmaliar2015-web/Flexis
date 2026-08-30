import { createTheme } from "@mui/material/styles";

export const theme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: "#0E2744",
      dark: "#091A2E",
      light: "#1C3F66",
      contrastText: "#F7F4EF",
    },
    secondary: {
      main: "#B08D57",
      dark: "#8C6C3F",
      light: "#C9AE7D",
      contrastText: "#1A140C",
    },
    background: {
      default: "#F4F1EB",
      paper: "#FFFcf7",
    },
    text: {
      primary: "#14202E",
      secondary: "#5C6672",
    },
    divider: "rgba(14, 39, 68, 0.1)",
  },
  typography: {
    fontFamily: '"Outfit", "Segoe UI", sans-serif',
    h1: {
      fontFamily: '"Fraunces", "Georgia", serif',
      fontWeight: 600,
      fontSize: "clamp(2.5rem, 6vw, 4.35rem)",
      letterSpacing: "-0.03em",
      lineHeight: 1.08,
    },
    h2: {
      fontFamily: '"Fraunces", "Georgia", serif',
      fontWeight: 600,
      fontSize: "clamp(1.6rem, 3vw, 2.1rem)",
      letterSpacing: "-0.02em",
      lineHeight: 1.2,
    },
    h4: {
      fontFamily: '"Fraunces", "Georgia", serif',
      fontWeight: 600,
      letterSpacing: "-0.02em",
    },
    h6: {
      fontWeight: 600,
      letterSpacing: "0.01em",
    },
    overline: {
      fontWeight: 600,
      letterSpacing: "0.2em",
      fontSize: "0.72rem",
    },
    button: {
      textTransform: "none",
      fontWeight: 600,
      letterSpacing: "0.01em",
    },
  },
  shape: {
    borderRadius: 12,
  },
  components: {
    MuiButton: {
      defaultProps: {
        variant: "contained",
        disableElevation: true,
      },
    },
    MuiAppBar: {
      defaultProps: {
        elevation: 0,
        color: "inherit",
      },
    },
  },
});
