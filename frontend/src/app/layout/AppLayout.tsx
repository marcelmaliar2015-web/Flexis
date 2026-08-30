import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { Link as RouterLink, Outlet } from "react-router-dom";

const ShellRoot = styled(Box)({
  minHeight: "100dvh",
  display: "flex",
  flexDirection: "column",
});

const ShellBar = styled(AppBar)(({ theme }) => ({
  backgroundColor: theme.palette.background.paper,
  color: theme.palette.text.primary,
  borderBottom: `1px solid ${theme.palette.divider}`,
  backgroundImage: "none",
}));

const BrandMark = styled("span")(({ theme }) => ({
  width: 18,
  height: 18,
  borderRadius: 5,
  display: "inline-block",
  flexShrink: 0,
  background: `linear-gradient(145deg, ${theme.palette.primary.main} 35%, ${theme.palette.secondary.main} 100%)`,
}));

const ShellMain = styled("main")({
  flex: 1,
  display: "flex",
  flexDirection: "column",
});

export function AppLayout() {
  return (
    <ShellRoot>
      <ShellBar position="sticky">
        <Toolbar>
          <Box
            component={RouterLink}
            to="/"
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 1.25,
              flexGrow: 1,
              minWidth: 0,
              textDecoration: "none",
              color: "inherit",
            }}
          >
            <BrandMark />
            <Typography variant="h6" component="span">
              Flexis
            </Typography>
          </Box>
          <Stack direction="row" spacing={0.5} sx={{ flexShrink: 0 }}>
            <Box sx={{ display: { xs: "none", sm: "block" } }}>
              <Button color="inherit" component={RouterLink} to="/" variant="text">
                Home
              </Button>
            </Box>
            <Button color="inherit" component={RouterLink} to="/health" variant="text">
              Health
            </Button>
          </Stack>
        </Toolbar>
      </ShellBar>
      <ShellMain>
        <Outlet />
      </ShellMain>
    </ShellRoot>
  );
}
