import Box from "@mui/material/Box";
import TextField from "@mui/material/TextField";
import { styled } from "@mui/material/styles";
import type { ProfileInfo } from "@/shared/types/jobCatalog";

const FieldGrid = styled(Box)(({ theme }) => ({
  display: "grid",
  gap: theme.spacing(2),
  gridTemplateColumns: "1fr",
  [theme.breakpoints.up("md")]: {
    gridTemplateColumns: "1fr 1fr",
  },
}));

type ProfileInfoFieldsProps = {
  value: ProfileInfo;
  onChange: (value: ProfileInfo) => void;
  disabled?: boolean;
};

export function ProfileInfoFields({ value, onChange, disabled = false }: ProfileInfoFieldsProps) {
  function setField<K extends keyof ProfileInfo>(key: K, fieldValue: ProfileInfo[K]) {
    onChange({ ...value, [key]: fieldValue });
  }

  return (
    <FieldGrid>
      <TextField
        label="Name"
        value={value.name}
        onChange={(event) => setField("name", event.target.value)}
        disabled={disabled}
        fullWidth
      />
      <TextField
        label="Mail"
        value={value.mail}
        onChange={(event) => setField("mail", event.target.value)}
        disabled={disabled}
        fullWidth
      />
      <TextField
        label="Phone"
        value={value.phone}
        onChange={(event) => setField("phone", event.target.value)}
        disabled={disabled}
        fullWidth
      />
      <TextField
        label="Password"
        type="password"
        value={value.password}
        onChange={(event) => setField("password", event.target.value)}
        disabled={disabled}
        fullWidth
        autoComplete="off"
      />
      <TextField
        label="LinkedIn"
        value={value.linkedIn}
        onChange={(event) => setField("linkedIn", event.target.value)}
        disabled={disabled}
        fullWidth
      />
      <TextField
        label="Target Rate (Monthly)"
        value={value.targetRateMonthly}
        onChange={(event) => setField("targetRateMonthly", event.target.value)}
        disabled={disabled}
        fullWidth
      />
      <TextField
        label="Sex"
        value={value.sex}
        onChange={(event) => setField("sex", event.target.value)}
        disabled={disabled}
        fullWidth
      />
      <TextField
        label="Race"
        value={value.race}
        onChange={(event) => setField("race", event.target.value)}
        disabled={disabled}
        fullWidth
      />
      <TextField
        label="Veteran Status"
        value={value.veteranStatus}
        onChange={(event) => setField("veteranStatus", event.target.value)}
        disabled={disabled}
        fullWidth
      />
      <TextField
        label="Address"
        value={value.address}
        onChange={(event) => setField("address", event.target.value)}
        disabled={disabled}
        fullWidth
        multiline
        minRows={2}
        sx={{ gridColumn: { md: "1 / -1" } }}
      />
    </FieldGrid>
  );
}
