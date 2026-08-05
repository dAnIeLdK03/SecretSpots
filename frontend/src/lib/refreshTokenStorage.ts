const REFRESH_TOKEN_KEY = "secretspots.refreshToken";

export function getRefreshToken(): string | null {
  if (typeof window === "undefined") {
    return null;
  }
  return window.localStorage.getItem(REFRESH_TOKEN_KEY) ?? window.sessionStorage.getItem(REFRESH_TOKEN_KEY);
}

// persist=true -> localStorage (survives closing the browser), persist=false -> sessionStorage
// (cleared when the tab/browser closes). Omitting persist keeps using whichever storage already
// holds the token, so token-rotation on refresh doesn't silently upgrade a session-only login.
export function setRefreshToken(token: string, persist?: boolean): void {
  if (typeof window === "undefined") {
    return;
  }
  const rememberMe = persist ?? window.localStorage.getItem(REFRESH_TOKEN_KEY) !== null;
  const [primary, secondary] = rememberMe
    ? [window.localStorage, window.sessionStorage]
    : [window.sessionStorage, window.localStorage];
  primary.setItem(REFRESH_TOKEN_KEY, token);
  secondary.removeItem(REFRESH_TOKEN_KEY);
}

export function clearRefreshToken(): void {
  if (typeof window === "undefined") {
    return;
  }
  window.localStorage.removeItem(REFRESH_TOKEN_KEY);
  window.sessionStorage.removeItem(REFRESH_TOKEN_KEY);
}
