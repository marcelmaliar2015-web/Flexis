import { deleteRequest, getJson, postJson, putJson } from "@/shared/api/client";
import type {
  JobCatalogItem,
  JobCatalogKind,
  JobCatalogWriteRequest,
} from "@/shared/types/jobCatalog";

export function jobCatalogQueryKey(kind: JobCatalogKind) {
  return ["job-catalog", kind] as const;
}

export function listJobCatalogItems(kind: JobCatalogKind): Promise<JobCatalogItem[]> {
  return getJson<JobCatalogItem[]>(`/api/job-application/${kind}`);
}

export function createJobCatalogItem(
  kind: JobCatalogKind,
  request: JobCatalogWriteRequest,
): Promise<JobCatalogItem> {
  return postJson<JobCatalogItem>(`/api/job-application/${kind}`, request);
}

export function updateJobCatalogItem(
  kind: JobCatalogKind,
  id: string,
  request: JobCatalogWriteRequest,
): Promise<JobCatalogItem> {
  return putJson<JobCatalogItem>(`/api/job-application/${kind}/${id}`, request);
}

export function deleteJobCatalogItem(kind: JobCatalogKind, id: string): Promise<void> {
  return deleteRequest(`/api/job-application/${kind}/${id}`);
}
