import { getJson } from "@/shared/api/client";
import type { HealthStatusDto } from "@/shared/types/health";

export function getHealthStatus(): Promise<HealthStatusDto> {
  return getJson<HealthStatusDto>("/api/health");
}
