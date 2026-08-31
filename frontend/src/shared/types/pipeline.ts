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
};

export type JobPipelineForwardResult = {
  archivedSheetName: string;
  mainSheetName: string;
};

export type JobPipelineBatchForwardResult = {
  forwarded: number;
};
