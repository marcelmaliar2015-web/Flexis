import { getJson, putJson } from "@/shared/api/client";
import type {
  JobResumeBoard,
  JobResumeOwnerOptionsWrite,
  JobResumeProfileWrite,
} from "@/shared/types/resume";

export const jobResumeQueryKey = ["job-application", "resume"] as const;

export function getJobResumeBoard(): Promise<JobResumeBoard> {
  return getJson<JobResumeBoard>("/api/job-application/resume");
}

export function updateJobResumeOwnerOptions(
  request: JobResumeOwnerOptionsWrite,
): Promise<JobResumeBoard> {
  return putJson<JobResumeBoard>("/api/job-application/resume/owner-options", request);
}

export function updateJobResumeProfile(
  profileId: string,
  request: JobResumeProfileWrite,
): Promise<JobResumeBoard> {
  return putJson<JobResumeBoard>(`/api/job-application/resume/profiles/${profileId}`, request);
}
