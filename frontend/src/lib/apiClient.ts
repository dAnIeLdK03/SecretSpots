import { useAuthStore } from "@/store/useAuthStore";
import { getCurrentLocale } from "@/lib/currentLocale";
import { getRefreshToken, setRefreshToken, clearRefreshToken } from "@/lib/refreshTokenStorage";

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`);
    this.name = "ApiError";
  }
}

export function getErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof ApiError && (err.problem.detail || err.problem.title)) {
    return err.message;
  }
  return fallback;
}

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "";

const AUTH_ENDPOINTS_EXEMPT_FROM_REFRESH = ["/auth/login", "/auth/register", "/auth/refresh"];

let refreshPromise: Promise<string> | null = null;

export function refreshSession(): Promise<string> {
  if (!refreshPromise) {
    refreshPromise = performRefresh().finally(() => {
      refreshPromise = null;
    });
  }
  return refreshPromise;
}

async function performRefresh(): Promise<string> {
  const run = async (): Promise<string> => {
    const refreshToken = getRefreshToken();
    if (!refreshToken) {
      throw new Error("No refresh token available");
    }

    const response = await fetch(new URL("/auth/refresh", API_BASE_URL), {
      method: "POST",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        "Accept-Language": getCurrentLocale(),
      },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      throw new Error("Refresh failed");
    }

    const tokens: { accessToken: string; refreshToken: string } = await response.json();
    setRefreshToken(tokens.refreshToken);
    useAuthStore.getState().setAccessToken(tokens.accessToken);
    return tokens.accessToken;
  };

  if (typeof navigator !== "undefined" && "locks" in navigator) {
    return navigator.locks.request("secretspots-refresh-token", run);
  }
  return run();
}

async function doFetch(path: string, options: RequestInit = {}, isRetry = false): Promise<Response> {
  if (!API_BASE_URL) {
    throw new Error("NEXT_PUBLIC_API_URL is not set");
  }

  const isFormData = options.body instanceof FormData;
  const accessToken = useAuthStore.getState().accessToken;

  const response = await fetch(new URL(path, API_BASE_URL), {
    ...options,
    credentials: "include",
    headers: {
      ...(isFormData ? {} : { "Content-Type": "application/json" }),
      "Accept-Language": getCurrentLocale(),
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...options.headers,
    },
  });

  if (
    response.status === 401 &&
    accessToken &&
    !isRetry &&
    !AUTH_ENDPOINTS_EXEMPT_FROM_REFRESH.includes(path)
  ) {
    try {
      await refreshSession();
    } catch {
      useAuthStore.getState().clearSession();
      clearRefreshToken();
      const problem: ProblemDetails = await response.json().catch(() => ({}));
      throw new ApiError(response.status, problem);
    }
    return doFetch(path, options, true);
  }

  if (!response.ok) {
    const problem: ProblemDetails = await response.json().catch(() => ({}));
    throw new ApiError(response.status, problem);
  }

  return response;
}

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await doFetch(path, options);
  return response.json() as Promise<T>;
}

export async function apiFetchVoid(path: string, options: RequestInit = {}): Promise<void> {
  await doFetch(path, options);
}

export interface HealthResponse {
  status: string;
}

export function getHealth(signal?: AbortSignal): Promise<HealthResponse> {
  return apiFetch<HealthResponse>("/health", { signal });
}
