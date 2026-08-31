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
};

export type JobFinancialBoard = {
  defaults: JobFinancialDefaults;
  rows: JobFinancialRow[];
  allPrice: number;
  allTotal: number;
  allApplied: number;
  allInterviews: number;
};

export type JobApplicationLog = {
  id: string;
  occurredAt: string;
  category: string;
  action: string;
  summary: string;
  detail: string;
};
