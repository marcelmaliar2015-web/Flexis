import { getApiBaseUrl } from "@/shared/config/env";
import { getAccessToken } from "@/shared/api/accessToken";
import { reportIssue } from "@/shared/notifications/issueStore";

export class ApiError extends Error {
  readonly status: number;
  readonly path: string;
  readonly method: string;

  constructor(message: string, status: number, path: string, method: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.path = path;
    this.method = method;
  }
}

export async function getJson<T>(path: string): Promise<T> {
  return requestJson<T>(path);
}

export async function postJson<T>(
  path: string,
  body: unknown,
  init?: Pick<RequestInit, "signal">,
): Promise<T> {
  return requestJson<T>(path, { method: "POST", body: JSON.stringify(body), ...init });
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
  const method = (init?.method ?? "GET").toUpperCase();
  const headers = new Headers(init?.headers);
  headers.set("Accept", "application/json");
  if (init?.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const token = getAccessToken();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const maxAttempts = 4;
  let lastNetworkError: unknown = null;
  let lastBadGateway: Response | null = null;

  for (let attempt = 1; attempt <= maxAttempts; attempt += 1) {
    try {
      const response = await fetch(`${getApiBaseUrl()}${path}`, { ...init, headers });
      if (
        (response.status === 502 || response.status === 504)
        && attempt < maxAttempts
      ) {
        lastBadGateway = response;
        await wait(attempt * 500);
        continue;
      }

      if (!response.ok && response.status !== 503) {
        if (response.status === 502 || response.status === 504) {
          const message =
            "Could not reach the API. Check backend/src/Flexis.Api is running on http://localhost:5080.";
          notifyApiFailure(message, response.status, path, method);
          throw new Error(message);
        }

        let body: unknown = null;
        try {
          body = await response.json();
        } catch {
          body = null;
        }

        const message = problemMessage(body, response.status, method, path);
        notifyApiFailure(message, response.status, path, method);
        throw new ApiError(message, response.status, path, method);
      }

      return response;
    } catch (error) {
      if (error instanceof ApiError || (error instanceof Error && error.message.startsWith("Could not reach the API"))) {
        throw error;
      }

      if (isAbortError(error)) {
        throw error;
      }

      lastNetworkError = error;
      if (attempt < maxAttempts) {
        await wait(attempt * 500);
        continue;
      }
    }
  }

  if (lastBadGateway) {
    const message =
      "Could not reach the API. Check backend/src/Flexis.Api is running on http://localhost:5080.";
    notifyApiFailure(message, lastBadGateway.status, path, method);
    throw new Error(message);
  }

  const message =
    "Could not reach the API. Check backend/src/Flexis.Api is running on http://localhost:5080.";
  notifyApiFailure(
    message,
    0,
    path,
    method,
    lastNetworkError instanceof Error
      ? `${lastNetworkError.name}: ${lastNetworkError.message}`
      : String(lastNetworkError),
  );
  throw new Error(message);
}

function wait(ms: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });
}

export function isAbortError(error: unknown): boolean {
  if (error instanceof DOMException && error.name === "AbortError") {
    return true;
  }
  return error instanceof Error && error.name === "AbortError";
}

function notifyApiFailure(
  message: string,
  status: number,
  path: string,
  method: string,
  detail?: string,
) {
  if (path === "/api/diagnostics/events") {
    return;
  }
  if (method === "GET" && path === "/api/auth/me" && status === 401) {
    return;
  }

  reportIssue({
    severity: "error",
    source: "api",
    message,
    method,
    path,
    status,
    detail,
  });
}

function problemMessage(body: unknown, status: number, method: string, path: string): string {
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

  if (status === 404) {
    return `${method} ${path} was not found. Restart backend/src/Flexis.Api so it matches this app.`;
  }

  return `${method} ${path} failed with status ${status}.`;
}
