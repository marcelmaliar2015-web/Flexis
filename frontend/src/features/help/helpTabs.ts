export const helpTabItems = [
  {
    value: "overview",
    label: "Overview",
    summary: "Map of Flexis: screens, header, roles, and first run.",
  },
  {
    value: "google",
    label: "Google setup",
    summary: "Google Cloud client, APIs, scopes, Drive folders, and Connect Gmail.",
  },
  {
    value: "operations",
    label: "Operations",
    summary: "Pipeline rows, Update, Forward, banned companies, and sheet lock.",
  },
  {
    value: "financial",
    label: "Financial",
    summary: "Listings, Applied, Interview, rates, and price.",
  },
  {
    value: "logs",
    label: "Logs",
    summary: "Dated activity for pipeline, catalog, financial, and Gmail.",
  },
  {
    value: "problems",
    label: "Problems",
    summary: "Fixes when connect, sheets, or price do not match what you expect.",
  },
] as const;

export type HelpTabValue = (typeof helpTabItems)[number]["value"];
