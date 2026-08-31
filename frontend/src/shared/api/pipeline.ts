import { deleteRequest, getJson, postJson, putJson } from "@/shared/api/client";
import type {
  JobPipelineBannedCompany,
  JobPipelineBannedCompanyWriteRequest,
  JobPipelineBannedMatches,
  JobPipelineBoard,
  JobPipelineEntry,
  JobPipelineForwardResult,
  JobPipelineBatchForwardResult,
  JobPipelineUpdateResult,
  JobPipelineWriteRequest,
} from "@/shared/types/pipeline";

export const jobPipelineQueryKey = ["job-pipeline"] as const;

export function getJobPipelineBoard(): Promise<JobPipelineBoard> {
  return getJson<JobPipelineBoard>("/api/job-application/pipeline");
}

export function createJobPipelineEntry(request: JobPipelineWriteRequest): Promise<JobPipelineEntry> {
  return postJson<JobPipelineEntry>("/api/job-application/pipeline", request);
}

export function updateJobPipelineEntry(
  id: string,
  request: JobPipelineWriteRequest,
): Promise<JobPipelineEntry> {
  return putJson<JobPipelineEntry>(`/api/job-application/pipeline/${id}`, request);
}

export function deleteJobPipelineEntry(id: string): Promise<void> {
  return deleteRequest(`/api/job-application/pipeline/${id}`);
}

export function deleteAllJobPipelineEntries(): Promise<void> {
  return deleteRequest("/api/job-application/pipeline");
}

export function applyJobPipelineEntry(id: string): Promise<JobPipelineUpdateResult> {
  return postJson<JobPipelineUpdateResult>(`/api/job-application/pipeline/${id}/update`, {});
}

export function applyAllJobPipelineEntries(): Promise<JobPipelineUpdateResult> {
  return postJson<JobPipelineUpdateResult>("/api/job-application/pipeline/update-all", {});
}

export function forwardJobPipelineEntry(id: string): Promise<JobPipelineForwardResult> {
  return postJson<JobPipelineForwardResult>(`/api/job-application/pipeline/${id}/forward`, {});
}

export function forwardAllJobPipelineEntries(): Promise<JobPipelineBatchForwardResult> {
  return postJson<JobPipelineBatchForwardResult>("/api/job-application/pipeline/forward-all", {});
}

export function jobPipelineBannedQueryKey(id: string) {
  return ["job-pipeline", id, "banned-companies"] as const;
}

export function jobPipelineBannedMatchesQueryKey(id: string) {
  return ["job-pipeline", id, "banned-matches"] as const;
}

export function listJobPipelineBannedCompanies(id: string): Promise<JobPipelineBannedCompany[]> {
  return getJson<JobPipelineBannedCompany[]>(`/api/job-application/pipeline/${id}/banned-companies`);
}

export function createJobPipelineBannedCompany(
  id: string,
  request: JobPipelineBannedCompanyWriteRequest,
): Promise<JobPipelineBannedCompany> {
  return postJson<JobPipelineBannedCompany>(`/api/job-application/pipeline/${id}/banned-companies`, request);
}

export function updateJobPipelineBannedCompany(
  id: string,
  companyId: string,
  request: JobPipelineBannedCompanyWriteRequest,
): Promise<JobPipelineBannedCompany> {
  return putJson<JobPipelineBannedCompany>(
    `/api/job-application/pipeline/${id}/banned-companies/${companyId}`,
    request,
  );
}

export function deleteJobPipelineBannedCompany(id: string, companyId: string): Promise<void> {
  return deleteRequest(`/api/job-application/pipeline/${id}/banned-companies/${companyId}`);
}

export function getJobPipelineBannedMatches(id: string): Promise<JobPipelineBannedMatches> {
  return getJson<JobPipelineBannedMatches>(`/api/job-application/pipeline/${id}/banned-matches`);
}
