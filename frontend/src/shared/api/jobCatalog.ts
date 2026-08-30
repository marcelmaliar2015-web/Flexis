import { deleteRequest, getJson, postJson, putJson } from "@/shared/api/client";
import type {
  JobCatalogItem,
  JobCatalogKind,
  JobCatalogWriteRequest,
  SourceLocation,
} from "@/shared/types/jobCatalog";

export function jobCatalogQueryKey(kind: JobCatalogKind) {
  return ["job-catalog", kind] as const;
}

export function sourceLocationsQueryKey(sourceId: string) {
  return ["job-catalog", "sources", sourceId, "locations"] as const;
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

export function listSourceLocations(sourceId: string): Promise<SourceLocation[]> {
  return getJson<SourceLocation[]>(`/api/job-application/sources/${sourceId}/locations`);
}

export function createSourceLocation(sourceId: string, name: string): Promise<SourceLocation> {
  return postJson<SourceLocation>(`/api/job-application/sources/${sourceId}/locations`, { name });
}

export function updateSourceLocation(
  sourceId: string,
  sheetId: number,
  name: string,
): Promise<SourceLocation> {
  return putJson<SourceLocation>(`/api/job-application/sources/${sourceId}/locations/${sheetId}`, {
    name,
  });
}

export function deleteSourceLocation(sourceId: string, sheetId: number): Promise<void> {
  return deleteRequest(`/api/job-application/sources/${sourceId}/locations/${sheetId}`);
}
