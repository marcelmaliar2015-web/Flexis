import type { QueryClient } from "@tanstack/react-query";
import { syncListingStatusBoards } from "@/shared/api/googleSync";
import { jobApplicationLogsRootQueryKey } from "@/shared/api/jobApplicationLogs";
import { requestSheetRefresh } from "@/shared/api/sheetRefreshCoordinator";

export async function refreshJobApplicationWorkspace(queryClient: QueryClient) {
  await requestSheetRefresh("workspace", async (onProgress) => {
    await syncListingStatusBoards(queryClient, onProgress, 0, 90);
    await queryClient.invalidateQueries({ queryKey: jobApplicationLogsRootQueryKey });
    onProgress?.(100);
  });
}
