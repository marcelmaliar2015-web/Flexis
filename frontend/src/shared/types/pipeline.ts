export type JobPipelineOption = {
  id: string;
  title: string;
};

export type JobPipelineSourceOption = {
  id: string;
  title: string;
  locations: { sheetId: number; name: string }[];
};

export type JobPipelineEntry = {
  id: string;
  profileId: string;
  sourceId: string;
  locationSheetId: number;
  locationName: string;
  createdAt: string;
};

export type JobPipelineBoard = {
  entries: JobPipelineEntry[];
  profiles: JobPipelineOption[];
  sources: JobPipelineSourceOption[];
};

export type JobPipelineWriteRequest = {
  profileId: string;
  sourceId: string;
  locationSheetId: number;
};

export type JobPipelineUpdateResult = {
  added: number;
  skipped: number;
  banned: number;
};

export type JobPipelineForwardResult = {
  archivedSheetName: string;
  mainSheetName: string;
};

export type JobPipelineBatchForwardResult = {
  forwarded: number;
};

export type JobPipelineBannedCompany = {
  id: string;
  companyName: string;
  createdAt: string;
};

export type JobPipelineBannedCompanyWriteRequest = {
  companyName: string;
};

export type JobPipelineBannedMatch = {
  sheet: "source" | "profile";
  companyName: string;
  position: string;
  link: string;
  matchedBan: string;
};

export type JobPipelineBannedMatches = {
  source: JobPipelineBannedMatch[];
  profile: JobPipelineBannedMatch[];
};
