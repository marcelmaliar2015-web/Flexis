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

export type ProfileInfo = {
  name: string;
  address: string;
  mail: string;
  password: string;
  linkedIn: string;
  phone: string;
  sex: string;
  targetRateMonthly: string;
  race: string;
  veteranStatus: string;
};

export type ProfileInfoWrite = {
  name: string;
  address: string;
  mail: string;
  password: string;
  linkedIn: string;
  phone: string;
  sex: string;
  targetRateMonthly: string;
  race: string;
  veteranStatus: string;
};

export const emptyProfileInfo = (): ProfileInfo => ({
  name: "",
  address: "",
  mail: "",
  password: "",
  linkedIn: "",
  phone: "",
  sex: "",
  targetRateMonthly: "",
  race: "",
  veteranStatus: "",
});
