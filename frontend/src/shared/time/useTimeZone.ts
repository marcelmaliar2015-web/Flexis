import { useAuth } from "@/shared/auth/AuthProvider";
import { resolveTimeZoneId } from "@/shared/time/timeZones";

export function useTimeZone(): string {
  const auth = useAuth();
  return resolveTimeZoneId(auth.user?.timeZoneId);
}
