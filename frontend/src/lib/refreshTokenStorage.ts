const REFRESH_TOKEN_KEY = "secretspots.refreshToken";

export function getRefreshToken(): string | null {
  if (typeof window === "undefined") {
    return null;
  }
  return window.localStorage.getItem(REFRESH_TOKEN_KEY);
}

export function setRefreshToken(token: string): void {
  if (typeof window === "undefined") {
    return;
  }
  window.localStorage.setItem(REFRESH_TOKEN_KEY, token);
}

export function clearRefreshToken(): void {
  if (typeof window === "undefined") {
    return;
  }
  window.localStorage.removeItem(REFRESH_TOKEN_KEY);
}
