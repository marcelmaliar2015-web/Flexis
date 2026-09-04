export const appPaths = {
  home: "/",
  signIn: "/sign-in",
  dashboard: "/dashboard",
  jobApplication: "/job-application",
  jobApplicationPipeline: (id: string) => `/job-application/pipeline/${id}`,
  mailCheck: "/mail-check",
  logs: "/logs",
  settings: "/settings",
  help: "/help",
  health: "/health",
  users: "/users",
} as const;
