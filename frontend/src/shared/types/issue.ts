export type IssueSeverity = "error" | "warning";

export type IssueNotice = {
  id: string;
  occurredAt: string;
  severity: IssueSeverity;
  source: string;
  message: string;
  method?: string;
  path?: string;
  status?: number;
  detail?: string;
};

export type IssueDraft = {
  severity: IssueSeverity;
  source: string;
  message: string;
  method?: string;
  path?: string;
  status?: number;
  detail?: string;
};
