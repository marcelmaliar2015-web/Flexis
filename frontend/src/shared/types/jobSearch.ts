export type JobSearchProfileOption = {
  profileId: string | null;
  title: string;
};

export type JobSearchMeta = {
  profiles: JobSearchProfileOption[];
  statuses: string[];
  searchBaseUrl: string;
  searchBaseSpreadsheetId: string;
};

export type JobSearchRequest = {
  profile: string | null;
  companyName: string | null;
  position: string | null;
  link: string | null;
  jd: string | null;
  status: string | null;
};

export type JobSearchHit = {
  rank: number;
  score: number;
  profile: string;
  companyName: string;
  position: string;
  link: string;
  jd: string;
  download: string;
  status: string;
  issue: string;
  source: "profile" | "search-base" | "profile-archive" | string;
  spreadsheetUrl: string;
  profileId: string | null;
};

export type JobSearchResult = {
  items: JobSearchHit[];
  scanned: number;
  searchBaseUrl: string;
};
