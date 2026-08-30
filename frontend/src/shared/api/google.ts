import { deleteRequest, getJson, postJson } from "@/shared/api/client";
import type { GoogleConnectStart, GoogleConnectionStatus } from "@/shared/types/google";

export const googleConnectionQueryKey = ["google-connection"] as const;

export function getGoogleConnection(): Promise<GoogleConnectionStatus> {
  return getJson<GoogleConnectionStatus>("/api/google/connections");
}

export function startGoogleConnection(returnUrl: string): Promise<GoogleConnectStart> {
  return postJson<GoogleConnectStart>("/api/google/connections/start", { returnUrl });
}

export function disconnectGoogleConnection(): Promise<void> {
  return deleteRequest("/api/google/connections");
}
