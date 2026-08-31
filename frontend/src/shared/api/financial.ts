import { getJson, putJson } from "@/shared/api/client";
import type {
  JobFinancialBoard,
  JobFinancialDefaults,
  JobFinancialRatesRequest,
  JobFinancialRow,
} from "@/shared/types/jobApplication";

export const jobFinancialQueryKey = ["job-financial"] as const;

export function getJobFinancialBoard(): Promise<JobFinancialBoard> {
  return getJson<JobFinancialBoard>("/api/job-application/financial");
}

export function updateJobFinancialDefaults(
  request: JobFinancialRatesRequest,
): Promise<JobFinancialDefaults> {
  return putJson<JobFinancialDefaults>("/api/job-application/financial/defaults", request);
}

export function updateJobFinancialRates(
  entryId: string,
  request: JobFinancialRatesRequest,
): Promise<JobFinancialRow> {
  return putJson<JobFinancialRow>(`/api/job-application/financial/rows/${entryId}/rates`, request);
}
