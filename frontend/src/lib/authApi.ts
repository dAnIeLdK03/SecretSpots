import { apiFetch, apiFetchVoid } from "@/lib/apiClient";
import { useAuthStore } from "@/store/useAuthStore";
import type { AuthUser } from "@/store/useAuthStore";
import { setRefreshToken } from "@/lib/refreshTokenStorage";

export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export function register(email: string, password: string, displayName: string): Promise<AuthResult> {
  return apiFetch<AuthResult>("/auth/register", {
    method: "POST",
    body: JSON.stringify({ email, password, displayName }),
  });
}

export function login(email: string, password: string): Promise<AuthResult> {
  return apiFetch<AuthResult>("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export function getCurrentUser(): Promise<AuthUser> {
  return apiFetch<AuthUser>("/auth/me");
}

export function logout(refreshToken: string): Promise<void> {
  return apiFetchVoid("/auth/logout", {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
}

export async function establishSession(result: AuthResult): Promise<void> {
  setRefreshToken(result.refreshToken);
  useAuthStore.getState().setAccessToken(result.accessToken);
  const user = await getCurrentUser();
  useAuthStore.getState().setSession(result.accessToken, user);
}
