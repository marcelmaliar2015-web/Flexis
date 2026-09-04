import type { QueryClient } from "@tanstack/react-query";
import { isApiError } from "@/shared/api/errors";
import {
  getJobFinancialBoard,
  getJobStatisticsBoard,
  jobFinancialQueryKey,
  jobStatisticsQueryKey,
} from "@/shared/api/financial";
import {
  getGoogleClient,
  getGoogleConnection,
  googleClientQueryKey,
  googleConnectionQueryKey,
} from "@/shared/api/google";
import { jobApplicationLogsQueryKey, listJobApplicationLogs } from "@/shared/api/jobApplicationLogs";
import {
  getProfileBannedMatches,
  jobCatalogQueryKey,
  listJobCatalogItems,
  profileBannedMatchesQueryKey,
} from "@/shared/api/jobCatalog";
import { getJobPipelineBoard, jobPipelineQueryKey } from "@/shared/api/pipeline";
import {
  requestSheetRefresh,
  type SheetRefreshKind,
  type SheetRefreshProgress,
} from "@/shared/api/sheetRefreshCoordinator";
import type { JobApplicationLogQuery } from "@/shared/types/jobApplication";

export const googleSyncIntervalMs = 300_000;

export const googleSyncFreshMs = 180_000;

export const googleSyncAgingMs = 600_000;

const bannedMatchGapMs = 2_500;

const syncLogsQuery: JobApplicationLogQuery = {
  page: 1,
  pageSize: 100,
};

export type GoogleSyncProgressReporter = SheetRefreshProgress;

type ProfileScanProgressReporter = (completed: number, total: number) => void;

function reportProgress(onProgress: GoogleSyncProgressReporter | undefined, percent: number): void {
  if (!onProgress) {
    return;
  }

  onProgress(Math.min(100, Math.max(0, Math.round(percent))));
}

function wait(ms: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });
}

async function syncAllProfileBannedMatches(
  queryClient: QueryClient,
  connected: boolean,
  onProfileProgress?: ProfileScanProgressReporter,
): Promise<void> {
  if (!connected) {
    onProfileProgress?.(1, 1);
    return;
  }

  const profiles = await queryClient.fetchQuery({
    queryKey: jobCatalogQueryKey("profiles"),
    queryFn: () => listJobCatalogItems("profiles"),
  });

  const total = profiles.length > 0 ? profiles.length : 1;
  if (profiles.length === 0) {
    onProfileProgress?.(1, 1);
    return;
  }

  for (let index = 0; index < profiles.length; index += 1) {
    try {
      await queryClient.fetchQuery({
        queryKey: profileBannedMatchesQueryKey(profiles[index].id),
        queryFn: () => getProfileBannedMatches(profiles[index].id),
      });
    } catch {
      continue;
    } finally {
      onProfileProgress?.(index + 1, total);
      if (index < profiles.length - 1) {
        await wait(bannedMatchGapMs);
      }
    }
  }
}

export async function syncListingStatusBoards(
  queryClient: QueryClient,
  onProgress?: GoogleSyncProgressReporter,
  fromPercent = 0,
  toPercent = 100,
): Promise<void> {
  const mid = fromPercent + (toPercent - fromPercent) / 2;
  await queryClient.fetchQuery({
    queryKey: jobFinancialQueryKey,
    queryFn: getJobFinancialBoard,
  });
  reportProgress(onProgress, mid);
  await queryClient.fetchQuery({
    queryKey: jobStatisticsQueryKey,
    queryFn: getJobStatisticsBoard,
  });
  reportProgress(onProgress, toPercent);
}

async function syncGoogleWorkspaceBody(
  queryClient: QueryClient,
  mode: Exclude<SheetRefreshKind, "workspace">,
  onProgress?: GoogleSyncProgressReporter,
): Promise<void> {
  const connection = await queryClient.fetchQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });
  if (mode === "auto") {
    reportProgress(onProgress, 10);
    if (!connection.connected) {
      reportProgress(onProgress, 100);
      return;
    }

    await syncListingStatusBoards(queryClient, onProgress, 10, 100);
    return;
  }

  reportProgress(onProgress, 6);
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

  reportProgress(onProgress, 14);
  await queryClient.fetchQuery({
    queryKey: jobPipelineQueryKey,
    queryFn: getJobPipelineBoard,
  });
  reportProgress(onProgress, 28);
  await queryClient.fetchQuery({
    queryKey: jobCatalogQueryKey("profiles"),
    queryFn: () => listJobCatalogItems("profiles"),
  });
  reportProgress(onProgress, 40);
  await queryClient.fetchQuery({
    queryKey: jobCatalogQueryKey("sources"),
    queryFn: () => listJobCatalogItems("sources"),
  });
  reportProgress(onProgress, 52);
  await queryClient.refetchQueries({
    predicate: (query) => {
      const root = query.queryKey[0];
      if (root === "job-catalog" && query.queryKey.length > 2) {
        return true;
      }
      return root === "job-pipeline" && query.queryKey.length > 1;
    },
  });
  reportProgress(onProgress, 58);
  await syncAllProfileBannedMatches(queryClient, connection.connected, (completed, total) => {
    reportProgress(onProgress, 58 + (22 * completed) / total);
  });
  reportProgress(onProgress, 82);
  await syncListingStatusBoards(queryClient, onProgress, 82, 96);
  await queryClient.fetchQuery({
    queryKey: jobApplicationLogsQueryKey(syncLogsQuery),
    queryFn: () => listJobApplicationLogs(syncLogsQuery),
  });
  reportProgress(onProgress, 100);
}

export async function syncGoogleWorkspace(
  queryClient: QueryClient,
  mode: "auto" | "manual",
  onProgress?: GoogleSyncProgressReporter,
): Promise<"completed" | "skipped"> {
  return requestSheetRefresh(
    mode,
    (progress) => syncGoogleWorkspaceBody(queryClient, mode, progress ?? onProgress),
    {
      skipIfFreshMs: mode === "auto" ? googleSyncIntervalMs : undefined,
      onProgress,
    },
  );
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

export function formatRelativeUpdatedAgo(lastSyncedAt: number, now: number): string {
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

export function formatUpdatedAgo(lastSyncedAt: number, now: number): string {
  const relative = formatRelativeUpdatedAgo(lastSyncedAt, now);
  if (relative === "Updated just now") {
    return "Sheet refresh · just now";
  }

  return relative.replace(/^Updated /, "Sheet refresh · ");
}
