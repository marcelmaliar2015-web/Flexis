import { getJson, putJson } from "@/shared/api/client";
import type {
  JobFinancialBoard,
  JobFinancialDefaults,
  JobFinancialRatesRequest,
  JobFinancialRow,
  JobFinancialSnapshot,
  JobStatisticsBoard,
  JobStatisticsPoint,
  JobStatisticsProfile,
} from "@/shared/types/jobApplication";

function asFiniteNumber(value: number | null | undefined, fallback = 0): number {
  return typeof value === "number" && Number.isFinite(value) ? value : fallback;
}

export const jobFinancialQueryKey = ["job-financial"] as const;

export const jobStatisticsQueryKey = ["job-statistics"] as const;

export async function getJobFinancialBoard(): Promise<JobFinancialBoard> {
  const board = await getJson<JobFinancialBoard>("/api/job-application/financial");
  return normalizeFinancialBoard(board);
}

export async function getJobStatisticsBoard(): Promise<JobStatisticsBoard> {
  const board = await getJson<JobStatisticsBoard>("/api/job-application/financial/statistics");
  return normalizeStatisticsBoard(board);
}

export function updateJobFinancialDefaults(
  request: JobFinancialRatesRequest,
): Promise<JobFinancialDefaults> {
  return putJson<JobFinancialDefaults>("/api/job-application/financial/defaults", request);
}

export async function updateJobFinancialRates(
  entryId: string,
  request: JobFinancialRatesRequest,
): Promise<JobFinancialRow> {
  const row = await putJson<JobFinancialRow>(
    `/api/job-application/financial/rows/${entryId}/rates`,
    request,
  );
  return normalizeFinancialRow(row);
}

function normalizeFinancialBoard(board: JobFinancialBoard): JobFinancialBoard {
  const rows = (board.rows ?? []).map(normalizeFinancialRow);
  return {
    ...board,
    defaults: {
      applyRate: asFiniteNumber(board.defaults?.applyRate),
      bonusRate: asFiniteNumber(board.defaults?.bonusRate),
    },
    rows,
    allPrice: asFiniteNumber(board.allPrice),
    allTotal: asFiniteNumber(board.allTotal),
    allReady: asFiniteNumber(board.allReady),
    allNotReady: asFiniteNumber(board.allNotReady),
    allApplied: asFiniteNumber(board.allApplied),
    allInterviews: asFiniteNumber(board.allInterviews),
    allUnapplied: asFiniteNumber(board.allUnapplied),
    todayAllPrice: asFiniteNumber(board.todayAllPrice),
    todayAllTotal: asFiniteNumber(board.todayAllTotal),
    todayAllReady: asFiniteNumber(board.todayAllReady),
    todayAllNotReady: asFiniteNumber(board.todayAllNotReady),
    todayAllApplied: asFiniteNumber(board.todayAllApplied),
    todayAllInterviews: asFiniteNumber(board.todayAllInterviews),
    todayAllUnapplied: asFiniteNumber(board.todayAllUnapplied),
    archivedAllPrice: asFiniteNumber(board.archivedAllPrice),
    archivedAllTotal: asFiniteNumber(board.archivedAllTotal),
    archivedAllReady: asFiniteNumber(board.archivedAllReady),
    archivedAllNotReady: asFiniteNumber(board.archivedAllNotReady),
    archivedAllApplied: asFiniteNumber(board.archivedAllApplied),
    archivedAllInterviews: asFiniteNumber(board.archivedAllInterviews),
    archivedAllUnapplied: asFiniteNumber(board.archivedAllUnapplied),
    lifetimeAllPrice: asFiniteNumber(board.lifetimeAllPrice),
    lifetimeAllTotal: asFiniteNumber(board.lifetimeAllTotal),
    lifetimeAllReady: asFiniteNumber(board.lifetimeAllReady),
    lifetimeAllNotReady: asFiniteNumber(board.lifetimeAllNotReady),
    lifetimeAllApplied: asFiniteNumber(board.lifetimeAllApplied),
    lifetimeAllInterviews: asFiniteNumber(board.lifetimeAllInterviews),
    lifetimeAllUnapplied: asFiniteNumber(board.lifetimeAllUnapplied),
    history: (board.history ?? []).map(normalizeFinancialSnapshot),
  };
}

function normalizeFinancialRow(row: JobFinancialRow): JobFinancialRow {
  return {
    ...row,
    total: asFiniteNumber(row.total),
    ready: asFiniteNumber(row.ready),
    notReady: asFiniteNumber(row.notReady),
    applied: asFiniteNumber(row.applied),
    interviews: asFiniteNumber(row.interviews),
    unapplied: asFiniteNumber(row.unapplied),
    applyRate: asFiniteNumber(row.applyRate),
    bonusRate: asFiniteNumber(row.bonusRate),
    price: asFiniteNumber(row.price),
    todayTotal: asFiniteNumber(row.todayTotal),
    todayReady: asFiniteNumber(row.todayReady),
    todayNotReady: asFiniteNumber(row.todayNotReady),
    todayApplied: asFiniteNumber(row.todayApplied),
    todayInterviews: asFiniteNumber(row.todayInterviews),
    todayUnapplied: asFiniteNumber(row.todayUnapplied),
    todayPrice: asFiniteNumber(row.todayPrice),
    archivedTotal: asFiniteNumber(row.archivedTotal),
    archivedReady: asFiniteNumber(row.archivedReady),
    archivedNotReady: asFiniteNumber(row.archivedNotReady),
    archivedApplied: asFiniteNumber(row.archivedApplied),
    archivedInterviews: asFiniteNumber(row.archivedInterviews),
    archivedUnapplied: asFiniteNumber(row.archivedUnapplied),
    archivedPrice: asFiniteNumber(row.archivedPrice),
    lifetimeTotal: asFiniteNumber(row.lifetimeTotal),
    lifetimeReady: asFiniteNumber(row.lifetimeReady),
    lifetimeNotReady: asFiniteNumber(row.lifetimeNotReady),
    lifetimeApplied: asFiniteNumber(row.lifetimeApplied),
    lifetimeInterviews: asFiniteNumber(row.lifetimeInterviews),
    lifetimeUnapplied: asFiniteNumber(row.lifetimeUnapplied),
    lifetimePrice: asFiniteNumber(row.lifetimePrice),
  };
}

function normalizeFinancialSnapshot(item: JobFinancialSnapshot): JobFinancialSnapshot {
  const hasMain = typeof item.mainPrice === "number" || typeof item.mainTotal === "number";
  if (!hasMain) {
    return {
      ...item,
      todayPrice: 0,
      todayTotal: 0,
      todayApplied: 0,
      todayInterviews: 0,
      mainPrice: asFiniteNumber(item.todayPrice),
      mainTotal: asFiniteNumber(item.todayTotal),
      mainApplied: asFiniteNumber(item.todayApplied),
      mainInterviews: asFiniteNumber(item.todayInterviews),
      archivedPrice: asFiniteNumber(item.archivedPrice),
      archivedTotal: asFiniteNumber(item.archivedTotal),
      archivedApplied: asFiniteNumber(item.archivedApplied),
      archivedInterviews: asFiniteNumber(item.archivedInterviews),
      lifetimePrice: asFiniteNumber(item.lifetimePrice),
      lifetimeTotal: asFiniteNumber(item.lifetimeTotal),
      lifetimeApplied: asFiniteNumber(item.lifetimeApplied),
      lifetimeInterviews: asFiniteNumber(item.lifetimeInterviews),
    };
  }

  return {
    ...item,
    todayPrice: asFiniteNumber(item.todayPrice),
    todayTotal: asFiniteNumber(item.todayTotal),
    todayApplied: asFiniteNumber(item.todayApplied),
    todayInterviews: asFiniteNumber(item.todayInterviews),
    mainPrice: asFiniteNumber(item.mainPrice),
    mainTotal: asFiniteNumber(item.mainTotal),
    mainApplied: asFiniteNumber(item.mainApplied),
    mainInterviews: asFiniteNumber(item.mainInterviews),
    archivedPrice: asFiniteNumber(item.archivedPrice),
    archivedTotal: asFiniteNumber(item.archivedTotal),
    archivedApplied: asFiniteNumber(item.archivedApplied),
    archivedInterviews: asFiniteNumber(item.archivedInterviews),
    lifetimePrice: asFiniteNumber(item.lifetimePrice),
    lifetimeTotal: asFiniteNumber(item.lifetimeTotal),
    lifetimeApplied: asFiniteNumber(item.lifetimeApplied),
    lifetimeInterviews: asFiniteNumber(item.lifetimeInterviews),
  };
}

function normalizeStatisticsBoard(board: JobStatisticsBoard): JobStatisticsBoard {
  const profiles = (board.profiles ?? []).map(normalizeStatisticsProfile);
  return {
    ...board,
    profiles,
    history: (board.history ?? []).map(normalizeStatisticsPoint),
    allApplied: asFiniteNumber(board.allApplied),
    allInterviews: asFiniteNumber(board.allInterviews),
    allUnapplied: asFiniteNumber(board.allUnapplied),
    allReady: asFiniteNumber(board.allReady),
    allNotReady: asFiniteNumber(board.allNotReady),
    allTotal: asFiniteNumber(board.allTotal),
    allPrice: asFiniteNumber(board.allPrice),
    todayAllApplied: asFiniteNumber(board.todayAllApplied),
    todayAllInterviews: asFiniteNumber(board.todayAllInterviews),
    todayAllUnapplied: asFiniteNumber(board.todayAllUnapplied),
    todayAllReady: asFiniteNumber(board.todayAllReady),
    todayAllNotReady: asFiniteNumber(board.todayAllNotReady),
    todayAllTotal: asFiniteNumber(board.todayAllTotal),
    todayAllPrice: asFiniteNumber(board.todayAllPrice),
  };
}

function normalizeStatisticsProfile(profile: JobStatisticsProfile): JobStatisticsProfile {
  return {
    ...profile,
    applied: asFiniteNumber(profile.applied),
    interviews: asFiniteNumber(profile.interviews),
    unapplied: asFiniteNumber(profile.unapplied),
    ready: asFiniteNumber(profile.ready),
    notReady: asFiniteNumber(profile.notReady),
    total: asFiniteNumber(profile.total),
    price: asFiniteNumber(profile.price),
    todayApplied: asFiniteNumber(profile.todayApplied),
    todayInterviews: asFiniteNumber(profile.todayInterviews),
    todayUnapplied: asFiniteNumber(profile.todayUnapplied),
    todayReady: asFiniteNumber(profile.todayReady),
    todayNotReady: asFiniteNumber(profile.todayNotReady),
    todayTotal: asFiniteNumber(profile.todayTotal),
    todayPrice: asFiniteNumber(profile.todayPrice),
    applyRate: asFiniteNumber(profile.applyRate),
    bonusRate: asFiniteNumber(profile.bonusRate),
  };
}

function normalizeStatisticsPoint(point: JobStatisticsPoint): JobStatisticsPoint {
  return {
    ...point,
    applied: asFiniteNumber(point.applied),
    interviews: asFiniteNumber(point.interviews),
    unapplied: asFiniteNumber(point.unapplied),
    total: asFiniteNumber(point.total),
    price: asFiniteNumber(point.price),
  };
}
