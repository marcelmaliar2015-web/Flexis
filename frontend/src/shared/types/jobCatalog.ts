export type JobCatalogItem = {
  id: string;
  title: string;
  createdAt: string;
  url: string;
  spreadsheetId: string;
};

export type JobCatalogWriteRequest = {
  title: string;
};

export type JobCatalogKind = "profiles" | "sources";

export type SourceLocation = {
  sheetId: number;
  name: string;
};
