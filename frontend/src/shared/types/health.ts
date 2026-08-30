export type HealthStatusDto = {
  status: string;
  checks: HealthCheckDto[];
};

export type HealthCheckDto = {
  name: string;
  status: string;
  description: string | null;
};
