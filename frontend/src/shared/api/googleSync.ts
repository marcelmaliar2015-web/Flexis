import type { QueryClient } from "@tanstack/react-query";
import { isApiError } from "@/shared/api/errors";
import { getJobFinancialBoard, jobFinancialQueryKey } from "@/shared/api/financial";
import {
  getGoogleClient,
  getGoogleConnection,
  googleClientQueryKey,
  googleConnectionQueryKey,
} from "@/shared/api/google";
import { jobApplicationLogsQueryKey, listJobApplicationLogs } from "@/shared/api/jobApplicationLogs";
import { jobCatalogQueryKey, listJobCatalogItems } from "@/shared/api/jobCatalog";
import { getJobPipelineBoard, jobPipelineQueryKey } from "@/shared/api/pipeline";

export const googleSyncIntervalMs = 180_000;

export const googleSyncFreshMs = 120_000;

export const googleSyncAgingMs = 480_000;

export async function syncGoogleWorkspace(
  queryClient: QueryClient,
  mode: "auto" | "manual",
): Promise<void> {
  const connection = await queryClient.fetchQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });
  if (mode === "auto") {
    if (!connection.connected) {
      return;
    }

    await queryClient.fetchQuery({
      queryKey: jobPipelineQueryKey,
      queryFn: getJobPipelineBoard,
    });
    await queryClient.fetchQuery({
      queryKey: jobFinancialQueryKey,
      queryFn: getJobFinancialBoard,
    });
    return;
  }

  try {
    await queryClient.fetchQuery({
      queryKey: googleClientQueryKey,
      queryFn: getGoogleClient,
    });
  } catch (error) {
    if (!(isApiError(error) && error.status === 403)) {
      throw error;
    }
  }

  await queryClient.fetchQuery({
    queryKey: jobPipelineQueryKey,
    queryFn: getJobPipelineBoard,
  });
  await queryClient.fetchQuery({
    queryKey: jobCatalogQueryKey("profiles"),
    queryFn: () => listJobCatalogItems("profiles"),
  });
  await queryClient.fetchQuery({
    queryKey: jobCatalogQueryKey("sources"),
    queryFn: () => listJobCatalogItems("sources"),
  });
  await queryClient.refetchQueries({
    predicate: (query) => {
      const root = query.queryKey[0];
      if (root === "job-catalog" && query.queryKey.length > 2) {
        return true;
      }
      return root === "job-pipeline" && query.queryKey.length > 1;
    },
  });
  await queryClient.fetchQuery({
    queryKey: jobFinancialQueryKey,
    queryFn: getJobFinancialBoard,
  });
  await queryClient.fetchQuery({
    queryKey: jobApplicationLogsQueryKey,
    queryFn: listJobApplicationLogs,
  });
}

export function googleSyncLane(
  lastSyncedAt: number | null,
  now: number,
  failed: boolean,
  isSyncing: boolean,
): "busy" | "fresh" | "aging" | "stale" {
  if (isSyncing) {
    return "busy";
  }
  if (failed || lastSyncedAt === null) {
    return "stale";
  }

  const age = now - lastSyncedAt;
  if (age < googleSyncFreshMs) {
    return "fresh";
  }
  if (age < googleSyncAgingMs) {
    return "aging";
  }
  return "stale";
}

export function formatUpdatedAgo(lastSyncedAt: number, now: number): string {
  const seconds = Math.max(0, Math.floor((now - lastSyncedAt) / 1000));
  if (seconds < 45) {
    return "Updated just now";
  }

  const minutes = Math.max(1, Math.round(seconds / 60));
  if (minutes < 60) {
    return minutes === 1 ? "Updated 1 min ago" : `Updated ${minutes} mins ago`;
  }

  const hours = Math.max(1, Math.round(minutes / 60));
  return hours === 1 ? "Updated 1 hr ago" : `Updated ${hours} hrs ago`;
}
