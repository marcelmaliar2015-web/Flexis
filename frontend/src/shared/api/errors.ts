import { ApiError } from "@/shared/api/client";

export function isApiError(error: unknown): error is ApiError {
  if (error instanceof ApiError) {
    return true;
  }
  if (!error || typeof error !== "object") {
    return false;
  }
  const record = error as Record<string, unknown>;
  return (
    record.name === "ApiError" &&
    typeof record.status === "number" &&
    typeof record.path === "string" &&
    typeof record.method === "string"
  );
}

function isReportedFailureMessage(message: string): boolean {
  const trimmed = message.trim();
  if (trimmed.length === 0) {
    return true;
  }
  if (trimmed.startsWith("Could not reach the API.")) {
    return true;
  }
  if (trimmed.includes(" was not found. Restart backend")) {
    return true;
  }
  if (trimmed.includes(" failed with status ")) {
    return true;
  }
  return false;
}

export function userFacingError(error: unknown): string | null {
  if (error == null) {
    return null;
  }
  if (isApiError(error)) {
    return null;
  }
  if (error instanceof Error) {
    if (isReportedFailureMessage(error.message)) {
      return null;
    }
    return error.message;
  }
  return null;
}
