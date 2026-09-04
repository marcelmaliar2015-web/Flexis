export type JobFinancialDefaults = {
  applyRate: number;
  bonusRate: number;
};

export type JobFinancialRatesRequest = {
  applyRate: number;
  bonusRate: number;
};

export type JobFinancialRow = {
  entryId: string;
  profileId: string;
  profileTitle: string;
  profileUrl: string;
  sourceLabel: string;
  total: number;
  ready: number;
  notReady: number;
  applied: number;
  interviews: number;
  unapplied: number;
  applyRate: number;
  bonusRate: number;
  price: number;
  todayTotal: number;
  todayReady: number;
  todayNotReady: number;
  todayApplied: number;
  todayInterviews: number;
  todayUnapplied: number;
  todayPrice: number;
  archivedTotal: number;
  archivedReady: number;
  archivedNotReady: number;
  archivedApplied: number;
  archivedInterviews: number;
  archivedUnapplied: number;
  archivedPrice: number;
  lifetimeTotal: number;
  lifetimeReady: number;
  lifetimeNotReady: number;
  lifetimeApplied: number;
  lifetimeInterviews: number;
  lifetimeUnapplied: number;
  lifetimePrice: number;
};

export type JobFinancialBoard = {
  defaults: JobFinancialDefaults;
  rows: JobFinancialRow[];
  allPrice: number;
  allTotal: number;
  allReady: number;
  allNotReady: number;
  allApplied: number;
  allInterviews: number;
  allUnapplied: number;
  todayAllPrice: number;
  todayAllTotal: number;
  todayAllReady: number;
  todayAllNotReady: number;
  todayAllApplied: number;
  todayAllInterviews: number;
  todayAllUnapplied: number;
  archivedAllPrice: number;
  archivedAllTotal: number;
  archivedAllReady: number;
  archivedAllNotReady: number;
  archivedAllApplied: number;
  archivedAllInterviews: number;
  archivedAllUnapplied: number;
  lifetimeAllPrice: number;
  lifetimeAllTotal: number;
  lifetimeAllReady: number;
  lifetimeAllNotReady: number;
  lifetimeAllApplied: number;
  lifetimeAllInterviews: number;
  lifetimeAllUnapplied: number;
  history: JobFinancialSnapshot[];
};

export type JobFinancialSnapshot = {
  capturedOn: string;
  capturedHour: string;
  capturedAt: string;
  todayPrice: number;
  todayTotal: number;
  todayApplied: number;
  todayInterviews: number;
  mainPrice: number;
  mainTotal: number;
  mainApplied: number;
  mainInterviews: number;
  archivedPrice: number;
  archivedTotal: number;
  archivedApplied: number;
  archivedInterviews: number;
  lifetimePrice: number;
  lifetimeTotal: number;
  lifetimeApplied: number;
  lifetimeInterviews: number;
};

export type JobStatisticsProfile = {
  profileId: string;
  profileTitle: string;
  profileUrl: string;
  applied: number;
  interviews: number;
  unapplied: number;
  ready: number;
  notReady: number;
  total: number;
  price: number;
  todayApplied: number;
  todayInterviews: number;
  todayUnapplied: number;
  todayReady: number;
  todayNotReady: number;
  todayTotal: number;
  todayPrice: number;
  applyRate: number;
  bonusRate: number;
};

export type JobStatisticsPoint = {
  profileId: string;
  profileTitle: string;
  capturedOn: string;
  capturedHour: string;
  applied: number;
  interviews: number;
  unapplied: number;
  total: number;
  price: number;
};

export type JobStatisticsBoard = {
  profiles: JobStatisticsProfile[];
  history: JobStatisticsPoint[];
  allApplied: number;
  allInterviews: number;
  allUnapplied: number;
  allReady: number;
  allNotReady: number;
  allTotal: number;
  allPrice: number;
  todayAllApplied: number;
  todayAllInterviews: number;
  todayAllUnapplied: number;
  todayAllReady: number;
  todayAllNotReady: number;
  todayAllTotal: number;
  todayAllPrice: number;
};

export type JobApplicationLog = {
  id: string;
  occurredAt: string;
  category: string;
  action: string;
  summary: string;
  detail: string;
};

export type JobApplicationLogQuery = {
  page?: number;
  pageSize?: number;
  category?: string | null;
  q?: string | null;
};

export type JobApplicationLogPage = {
  items: JobApplicationLog[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};
