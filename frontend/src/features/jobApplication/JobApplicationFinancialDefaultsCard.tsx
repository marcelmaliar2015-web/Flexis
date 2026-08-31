import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { styled } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import {
  getJobFinancialBoard,
  jobFinancialQueryKey,
  updateJobFinancialDefaults,
} from "@/shared/api/financial";
import { formatRate, parseRate } from "@/features/jobApplication/financialUi";
import { errorMessage } from "@/features/jobApplication/pipelineUi";
import { refreshJobApplicationWorkspace } from "@/features/jobApplication/refreshWorkspace";

const Panel = styled(Box)(({ theme }) => ({
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(3.5),
}));

const RateField = styled(TextField)({
  maxWidth: 220,
});

export function JobApplicationFinancialDefaultsCard() {
  const queryClient = useQueryClient();
  const boardQuery = useQuery({
    queryKey: jobFinancialQueryKey,
    queryFn: getJobFinancialBoard,
  });
  const [applyRate, setApplyRate] = useState("");
  const [bonusRate, setBonusRate] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    const defaults = boardQuery.data?.defaults;
    if (!defaults) {
      return;
    }

    setApplyRate(formatRate(defaults.applyRate));
    setBonusRate(formatRate(defaults.bonusRate));
  }, [boardQuery.data?.defaults]);

  const saveMutation = useMutation({
    mutationFn: updateJobFinancialDefaults,
    onSuccess: async (result) => {
      setFormError(null);
      setSaved(true);
      setApplyRate(formatRate(result.applyRate));
      setBonusRate(formatRate(result.bonusRate));
      await refreshJobApplicationWorkspace(queryClient);
    },
    onError: (error) => {
      setSaved(false);
      setFormError(errorMessage(error));
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextApply = parseRate(applyRate);
    const nextBonus = parseRate(bonusRate);
    if (nextApply === null || nextBonus === null) {
      setSaved(false);
      setFormError("Apply rate and bonus rate must be numbers from 0 to 10000.");
      return;
    }

    saveMutation.mutate({ applyRate: nextApply, bonusRate: nextBonus });
  }

  return (
    <Panel>
      <Box component="form" onSubmit={handleSubmit}>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Financial defaults
            </Typography>
            <Typography variant="body2" color="text.secondary">
              New pipeline rows start with these apply and bonus rates. Existing Financial rows keep the rates already
              saved on them.
            </Typography>
          </Stack>
          {errorMessage(boardQuery.error) ? <Alert severity="error">{errorMessage(boardQuery.error)}</Alert> : null}
          {formError ? <Alert severity="error">{formError}</Alert> : null}
          {saved && !formError ? <Alert severity="success">Default rates saved.</Alert> : null}
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <RateField
              label="Apply rate"
              value={applyRate}
              onChange={(event) => {
                setSaved(false);
                setApplyRate(event.target.value);
              }}
              disabled={boardQuery.isLoading || saveMutation.isPending}
              slotProps={{ htmlInput: { inputMode: "decimal", step: "0.0001", min: "0" } }}
            />
            <RateField
              label="Bonus rate"
              value={bonusRate}
              onChange={(event) => {
                setSaved(false);
                setBonusRate(event.target.value);
              }}
              disabled={boardQuery.isLoading || saveMutation.isPending}
              slotProps={{ htmlInput: { inputMode: "decimal", step: "0.0001", min: "0" } }}
            />
          </Stack>
          <Stack direction="row" spacing={1}>
            <Button type="submit" disabled={boardQuery.isLoading || saveMutation.isPending} loading={saveMutation.isPending}>
              Save defaults
            </Button>
          </Stack>
        </Stack>
      </Box>
    </Panel>
  );
}
