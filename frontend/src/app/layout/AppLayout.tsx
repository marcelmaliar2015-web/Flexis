import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import { Link as RouterLink, Outlet } from "react-router-dom";

export function AppLayout() {
  return (
    <Box>
      <AppBar position="static">
        <Toolbar>
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h6" component="div">
              Flexis
            </Typography>
          </Box>
          <Button color="inherit" component={RouterLink} to="/" variant="text">
            Home
          </Button>
          <Button color="inherit" component={RouterLink} to="/health" variant="text">
            Health
          </Button>
        </Toolbar>
      </AppBar>
      <Box component="main" sx={{ p: 3 }}>
        <Outlet />
      </Box>
    </Box>
  );
}
