import { getApiBaseUrl } from "@/shared/config/env";
import { getAccessToken } from "@/shared/api/accessToken";

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export async function getJson<T>(path: string): Promise<T> {
  return requestJson<T>(path);
}

export async function postJson<T>(path: string, body: unknown): Promise<T> {
  return requestJson<T>(path, { method: "POST", body: JSON.stringify(body) });
}

export async function putJson<T>(path: string, body: unknown): Promise<T> {
  return requestJson<T>(path, { method: "PUT", body: JSON.stringify(body) });
}

export async function deleteRequest(path: string): Promise<void> {
  const response = await send(path, { method: "DELETE" });
  if (response.status === 204) {
    return;
  }

  if (response.status === 503) {
    return;
  }
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await send(path, init);
  return (await response.json()) as T;
}

async function send(path: string, init?: RequestInit): Promise<Response> {
  const headers = new Headers(init?.headers);
  headers.set("Accept", "application/json");
  if (init?.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const token = getAccessToken();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  let response: Response;
  try {
    response = await fetch(`${getApiBaseUrl()}${path}`, { ...init, headers });
  } catch {
    throw new Error("API is not running. Start backend/src/Flexis.Api.");
  }

  if (!response.ok && response.status !== 503) {
    if (response.status === 502 || response.status === 504) {
      throw new Error("API is not running. Start backend/src/Flexis.Api.");
    }

    let body: unknown = null;
    try {
      body = await response.json();
    } catch {
      body = null;
    }

    throw new ApiError(problemMessage(body, response.status), response.status);
  }

  return response;
}

function problemMessage(body: unknown, status: number): string {
  if (body && typeof body === "object") {
    const record = body as { detail?: unknown; title?: unknown; message?: unknown };
    if (typeof record.detail === "string" && record.detail.length > 0) {
      return record.detail;
    }
    if (typeof record.title === "string" && record.title.length > 0) {
      return record.title;
    }
    if (typeof record.message === "string" && record.message.length > 0) {
      return record.message;
    }
  }

  return `Request failed with status ${status}.`;
}
