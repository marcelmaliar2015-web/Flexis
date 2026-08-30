const storageKey = "flexis.accessToken";

export function readStoredAccessToken(): string | null {
  return window.localStorage.getItem(storageKey);
}

export function writeStoredAccessToken(token: string | null): void {
  if (token) {
    window.localStorage.setItem(storageKey, token);
    return;
  }

  window.localStorage.removeItem(storageKey);
}
