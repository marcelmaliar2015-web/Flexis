import { getJson } from "@/shared/api/client";
import type { HealthStatusDto } from "@/shared/types/health";

export const healthQueryKey = ["health"] as const;

export function getHealthStatus(): Promise<HealthStatusDto> {
  return getJson<HealthStatusDto>("/api/health");
}
