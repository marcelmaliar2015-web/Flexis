export type JobResumeProfileRow = {
  profileId: string;
  title: string;
  url: string;
  prompt: string;
  resumeStyle: number | null;
  owner: string;
};

export type JobResumeBoard = {
  jobMasterUrl: string | null;
  ownerOptions: string[];
  profiles: JobResumeProfileRow[];
};

export type JobResumeOwnerOptionsWrite = {
  ownerOptions: string[];
};

export type JobResumeProfileWrite = {
  prompt?: string | null;
  resumeStyle?: number | null;
  owner?: string | null;
};
