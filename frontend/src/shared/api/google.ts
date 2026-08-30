import { deleteRequest, getJson, postJson, putJson } from "@/shared/api/client";
import type {
  GoogleClientSettings,
  GoogleConnectStart,
  GoogleConnectionStatus,
} from "@/shared/types/google";

export const googleConnectionQueryKey = ["google-connection"] as const;

export const googleClientQueryKey = ["google-client"] as const;

export function getGoogleConnection(): Promise<GoogleConnectionStatus> {
  return getJson<GoogleConnectionStatus>("/api/google/connections");
}

export function startGoogleConnection(returnUrl: string): Promise<GoogleConnectStart> {
  return postJson<GoogleConnectStart>("/api/google/connections/start", { returnUrl });
}

export function disconnectGoogleConnection(): Promise<void> {
  return deleteRequest("/api/google/connections");
}

export function getGoogleClient(): Promise<GoogleClientSettings> {
  return getJson<GoogleClientSettings>("/api/google/client");
}

export function saveGoogleClient(request: {
  clientId: string;
  clientSecret: string | null;
}): Promise<GoogleClientSettings> {
  return putJson<GoogleClientSettings>("/api/google/client", request);
}
