import Alert from "@mui/material/Alert";
import Stack from "@mui/material/Stack";
import { useQuery } from "@tanstack/react-query";
import { CatalogItemsPanel } from "@/features/jobApplication/CatalogItemsPanel";
import { JobApplicationFinancialDefaultsCard } from "@/features/jobApplication/JobApplicationFinancialDefaultsCard";
import { JobApplicationGmailCard } from "@/features/jobApplication/JobApplicationGmailCard";
import { ProfileInfoPanel } from "@/features/jobApplication/ProfileInfoPanel";
import { SourceLocationsPanel } from "@/features/jobApplication/SourceLocationsPanel";
import { getGoogleConnection, googleConnectionQueryKey } from "@/shared/api/google";

export function JobApplicationSettingsTab() {
  const connectionQuery = useQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });
  const connected = connectionQuery.data?.connected === true;

  return (
    <Stack spacing={4}>
      <JobApplicationGmailCard />
      <JobApplicationFinancialDefaultsCard />
      {!connected ? (
        <Alert severity="info">Connect Gmail to create, edit, or delete profiles and sources.</Alert>
      ) : null}
      <CatalogItemsPanel
        kind="profiles"
        heading="Profiles"
        itemLabel="profile"
        actionsEnabled={connected}
      />
      <ProfileInfoPanel actionsEnabled={connected} />
      <CatalogItemsPanel
        kind="sources"
        heading="Sources"
        itemLabel="source"
        actionsEnabled={connected}
      />
      <SourceLocationsPanel actionsEnabled={connected} />
    </Stack>
  );
}
