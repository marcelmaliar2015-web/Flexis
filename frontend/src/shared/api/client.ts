import { getApiBaseUrl } from "@/shared/config/env";

export async function getJson<T>(path: string): Promise<T> {
  const url = `${getApiBaseUrl()}${path}`;
  let response: Response;
  try {
    response = await fetch(url);
  } catch {
    throw new Error("API is not running. Start backend/src/Flexis.Api.");
  }

  if (!response.ok && response.status !== 503) {
    if (response.status === 502 || response.status === 504) {
      throw new Error("API is not running. Start backend/src/Flexis.Api.");
    }
    throw new Error(`Request failed with status ${response.status}.`);
  }

  return (await response.json()) as T;
}
