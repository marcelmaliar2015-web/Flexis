import { postJson } from "@/shared/api/client";

export type JobListingSyncResult = {
  checked: number;
  pulled: number;
};

export const jobListingSyncQueryKey = ["job-listing-sync"] as const;

export function syncJobListingProjections(): Promise<JobListingSyncResult> {
  return postJson<JobListingSyncResult>("/api/job-application/listings/sync", {});
}

export function reindexJobListingProjections(): Promise<JobListingSyncResult> {
  return postJson<JobListingSyncResult>("/api/job-application/listings/reindex", {});
}
