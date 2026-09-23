import { getJson, postJson } from "@/shared/api/client";
import type { JobSearchMeta, JobSearchRequest, JobSearchResult } from "@/shared/types/jobSearch";

export const jobSearchMetaQueryKey = ["job-search-meta"] as const;

export function getJobSearchMeta(): Promise<JobSearchMeta> {
  return getJson<JobSearchMeta>("/api/job-application/search");
}

export function searchJobListings(request: JobSearchRequest): Promise<JobSearchResult> {
  return postJson<JobSearchResult>("/api/job-application/search", request);
}
