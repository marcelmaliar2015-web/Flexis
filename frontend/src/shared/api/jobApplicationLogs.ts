import { getJson } from "@/shared/api/client";
import type { JobApplicationLogPage, JobApplicationLogQuery } from "@/shared/types/jobApplication";

export const jobApplicationLogsRootQueryKey = ["job-application-logs"] as const;

export const jobApplicationLogsQueryKey = (query: JobApplicationLogQuery) =>
  [...jobApplicationLogsRootQueryKey, query] as const;

export function listJobApplicationLogs(query: JobApplicationLogQuery): Promise<JobApplicationLogPage> {
  const params = new URLSearchParams();
  params.set("page", String(query.page ?? 1));
  params.set("pageSize", String(query.pageSize ?? 50));
  if (query.category && query.category !== "all") {
    params.set("category", query.category);
  }
  if (query.q?.trim()) {
    params.set("q", query.q.trim());
  }
  return getJson<JobApplicationLogPage>(`/api/job-application/logs?${params.toString()}`);
}
