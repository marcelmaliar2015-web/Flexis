import { getJson } from "@/shared/api/client";
import type { JobApplicationLog } from "@/shared/types/jobApplication";

export const jobApplicationLogsQueryKey = ["job-application-logs"] as const;

export function listJobApplicationLogs(): Promise<JobApplicationLog[]> {
  return getJson<JobApplicationLog[]>("/api/job-application/logs");
}
