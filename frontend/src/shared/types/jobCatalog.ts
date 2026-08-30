export type JobCatalogItem = {
  id: string;
  title: string;
  createdAt: string;
  url: string;
};

export type JobCatalogWriteRequest = {
  title: string;
  url: string;
};

export type JobCatalogKind = "profiles" | "sources";
