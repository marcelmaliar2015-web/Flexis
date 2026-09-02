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
  profileTitle: string;
  sourceLabel: string;
  total: number;
  applied: number;
  interviews: number;
  applyRate: number;
  bonusRate: number;
  price: number;
  archivedTotal: number;
  archivedApplied: number;
  archivedInterviews: number;
  archivedPrice: number;
  lifetimeTotal: number;
  lifetimeApplied: number;
  lifetimeInterviews: number;
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
};

export type JobApplicationLog = {
  id: string;
  occurredAt: string;
  category: string;
  action: string;
  summary: string;
  detail: string;
};
