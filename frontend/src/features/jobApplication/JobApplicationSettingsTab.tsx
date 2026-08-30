import Stack from "@mui/material/Stack";
import { CatalogItemsPanel } from "@/features/jobApplication/CatalogItemsPanel";
import { JobApplicationGmailCard } from "@/features/jobApplication/JobApplicationGmailCard";

export function JobApplicationSettingsTab() {
  return (
    <Stack spacing={4}>
      <JobApplicationGmailCard />
      <CatalogItemsPanel kind="profiles" heading="Profiles" itemLabel="profile" />
      <CatalogItemsPanel kind="sources" heading="Sources" itemLabel="source" />
    </Stack>
  );
}
