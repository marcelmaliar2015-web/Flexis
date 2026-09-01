import { deleteRequest, getJson, postJson, putJson } from "@/shared/api/client";
import type {
  JobCatalogItem,
  JobCatalogKind,
  JobCatalogWriteRequest,
  ProfileBannedCompany,
  ProfileBannedCompanyWrite,
  ProfileBannedMatches,
  ProfileInfo,
  ProfileInfoWrite,
  SourceLocation,
} from "@/shared/types/jobCatalog";

export function jobCatalogQueryKey(kind: JobCatalogKind) {
  return ["job-catalog", kind] as const;
}

export function sourceLocationsQueryKey(sourceId: string) {
  return ["job-catalog", "sources", sourceId, "locations"] as const;
}

export function profileInfoQueryKey(profileId: string) {
  return ["job-catalog", "profiles", profileId, "info"] as const;
}

export function profileBannedQueryKey(profileId: string) {
  return ["job-catalog", "profiles", profileId, "banned-companies"] as const;
}

export function profileBannedMatchesQueryKey(profileId: string) {
  return ["job-catalog", "profiles", profileId, "banned-matches"] as const;
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

export function getProfileInfo(profileId: string): Promise<ProfileInfo> {
  return getJson<ProfileInfo>(`/api/job-application/profiles/${profileId}/info`);
}

export function updateProfileInfo(profileId: string, request: ProfileInfoWrite): Promise<ProfileInfo> {
  return putJson<ProfileInfo>(`/api/job-application/profiles/${profileId}/info`, request);
}

export function listProfileBannedCompanies(profileId: string): Promise<ProfileBannedCompany[]> {
  return getJson<ProfileBannedCompany[]>(`/api/job-application/profiles/${profileId}/banned-companies`);
}

export function createProfileBannedCompany(
  profileId: string,
  request: ProfileBannedCompanyWrite,
): Promise<ProfileBannedCompany> {
  return postJson<ProfileBannedCompany>(
    `/api/job-application/profiles/${profileId}/banned-companies`,
    request,
  );
}

export function updateProfileBannedCompany(
  profileId: string,
  companyId: string,
  request: ProfileBannedCompanyWrite,
): Promise<ProfileBannedCompany> {
  return putJson<ProfileBannedCompany>(
    `/api/job-application/profiles/${profileId}/banned-companies/${companyId}`,
    request,
  );
}

export function deleteProfileBannedCompany(profileId: string, companyId: string): Promise<void> {
  return deleteRequest(`/api/job-application/profiles/${profileId}/banned-companies/${companyId}`);
}

export function getProfileBannedMatches(profileId: string): Promise<ProfileBannedMatches> {
  return getJson<ProfileBannedMatches>(`/api/job-application/profiles/${profileId}/banned-matches`);
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
