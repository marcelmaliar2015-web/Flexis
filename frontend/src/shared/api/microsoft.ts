import { getJson, putJson } from "@/shared/api/client";
import type { MicrosoftClientSettings } from "@/shared/types/microsoft";

export const microsoftClientQueryKey = ["microsoft-client"] as const;

export function getMicrosoftClient(): Promise<MicrosoftClientSettings> {
  return getJson<MicrosoftClientSettings>("/api/microsoft/client");
}

export function saveMicrosoftClient(request: {
  clientId: string;
  clientSecret: string | null;
}): Promise<MicrosoftClientSettings> {
  return putJson<MicrosoftClientSettings>("/api/microsoft/client", request);
}
