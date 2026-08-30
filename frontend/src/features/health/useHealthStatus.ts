import { useQuery } from "@tanstack/react-query";
import { getHealthStatus } from "@/shared/api/health";

export function useHealthStatus() {
  return useQuery({
    queryKey: ["health"],
    queryFn: getHealthStatus,
  });
}
