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

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "";

async function doFetch(path: string, options: RequestInit = {}): Promise<Response> {
  if (!API_BASE_URL) {
    throw new Error("NEXT_PUBLIC_API_URL is not set");
  }

  const isFormData = options.body instanceof FormData;

  const response = await fetch(new URL(path, API_BASE_URL), {
    ...options,
    credentials: "include",
    headers: {
      ...(isFormData ? {} : { "Content-Type": "application/json" }),
      ...options.headers,
    },
  });

  if (!response.ok) {
    const problem: ProblemDetails = await response.json().catch(() => ({}));
    throw new ApiError(response.status, problem);
  }

  return response;
}

// For endpoints that always return a JSON body (200/201 with content).
export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await doFetch(path, options);
  return response.json() as Promise<T>;
}

// For endpoints that return no body (e.g. 204 No Content on delete) — kept
// separate from apiFetch<T> so the return type never has to lie about a T
// that isn't actually there.
export async function apiFetchVoid(path: string, options: RequestInit = {}): Promise<void> {
  await doFetch(path, options);
}

export interface HealthResponse {
  status: string;
}

export function getHealth(signal?: AbortSignal): Promise<HealthResponse> {
  return apiFetch<HealthResponse>("/health", { signal });
}
