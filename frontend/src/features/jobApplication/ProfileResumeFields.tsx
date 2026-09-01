import Autocomplete from "@mui/material/Autocomplete";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";

const resumeStyleOptions = Array.from({ length: 14 }, (_, index) => index + 1);

type ProfileResumeFieldsProps = {
  prompt: string;
  resumeStyle: number | "";
  owner: string;
  ownerOptions: string[];
  onPromptChange: (value: string) => void;
  onResumeStyleChange: (value: number | "") => void;
  onOwnerChange: (value: string) => void;
  disabled?: boolean;
};

export function ProfileResumeFields({
  prompt,
  resumeStyle,
  owner,
  ownerOptions,
  onPromptChange,
  onResumeStyleChange,
  onOwnerChange,
  disabled = false,
}: ProfileResumeFieldsProps) {
  return (
    <Stack spacing={2}>
      <TextField
        label="Prompt"
        value={prompt}
        onChange={(event) => onPromptChange(event.target.value)}
        multiline
        minRows={8}
        maxRows={16}
        disabled={disabled}
        fullWidth
        placeholder="Resume generation instructions for this profile..."
      />
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ alignItems: { md: "flex-start" } }}
      >
        <TextField
          select
          label="Resume style"
          value={resumeStyle}
          onChange={(event) => {
            const next = event.target.value;
            onResumeStyleChange(next === "" ? "" : Number(next));
          }}
          disabled={disabled}
          sx={{ minWidth: { md: 200 } }}
          fullWidth
        >
          <MenuItem value="">None</MenuItem>
          {resumeStyleOptions.map((value) => (
            <MenuItem key={value} value={value}>
              {value}
            </MenuItem>
          ))}
        </TextField>
        <Autocomplete
          options={ownerOptions}
          value={owner.length > 0 ? owner : null}
          onChange={(_event, value) => onOwnerChange(value ?? "")}
          disabled={disabled}
          fullWidth
          renderInput={(params) => (
            <TextField
              {...params}
              label="Owner"
              helperText={
                ownerOptions.length === 0
                  ? "Add owner options on the Resume generation tab before selecting an owner."
                  : "Choose from your saved owner options."
              }
            />
          )}
        />
      </Stack>
    </Stack>
  );
}
