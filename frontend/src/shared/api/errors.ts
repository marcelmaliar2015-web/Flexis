import { ApiError } from "@/shared/api/client";

export function userFacingError(error: unknown): string | null {
  if (error instanceof ApiError) {
    return null;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return "Request failed.";
}
