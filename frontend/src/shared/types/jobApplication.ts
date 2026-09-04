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
  applied: number;
  interviews: number;
  unapplied: number;
  applyRate: number;
  bonusRate: number;
  price: number;
  archivedTotal: number;
  archivedApplied: number;
  archivedInterviews: number;
  archivedUnapplied: number;
  archivedPrice: number;
  lifetimeTotal: number;
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
  allApplied: number;
  allInterviews: number;
  archivedAllPrice: number;
  archivedAllTotal: number;
  archivedAllApplied: number;
  archivedAllInterviews: number;
  lifetimeAllPrice: number;
  lifetimeAllTotal: number;
  lifetimeAllApplied: number;
  lifetimeAllInterviews: number;
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
  total: number;
  price: number;
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
  allTotal: number;
  allPrice: number;
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
