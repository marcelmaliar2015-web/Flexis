import { useQuery } from "@tanstack/react-query";
import { getHealthStatus, healthQueryKey } from "@/shared/api/health";

export function useHealthStatus() {
  return useQuery({
    queryKey: healthQueryKey,
    queryFn: getHealthStatus,
  });
}
