import type { QueryClient } from "@tanstack/react-query";
import { jobFinancialQueryKey, jobStatisticsQueryKey } from "@/shared/api/financial";
import { jobApplicationLogsRootQueryKey } from "@/shared/api/jobApplicationLogs";

export function refreshJobApplicationWorkspace(queryClient: QueryClient) {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: jobFinancialQueryKey }),
    queryClient.invalidateQueries({ queryKey: jobStatisticsQueryKey }),
    queryClient.invalidateQueries({ queryKey: jobApplicationLogsRootQueryKey }),
  ]);
}
