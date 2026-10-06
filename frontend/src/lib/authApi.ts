import { apiFetch, apiFetchVoid } from "@/lib/apiClient";
import { useAuthStore } from "@/store/useAuthStore";
import type { AuthUser } from "@/store/useAuthStore";

export interface AuthResult {
  accessToken: string;
  expiresAt: string;
}

export function register(email: string, password: string, displayName: string): Promise<AuthResult> {
  return apiFetch<AuthResult>("/auth/register", {
    method: "POST",
    body: JSON.stringify({ email, password, displayName }),
  });
}

export function login(email: string, password: string, rememberMe: boolean): Promise<AuthResult> {
  return apiFetch<AuthResult>("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password, rememberMe }),
  });
}

export function exchangeExternalAuthCode(code: string): Promise<AuthResult> {
  return apiFetch<AuthResult>("/auth/external/exchange", {
    method: "POST",
    body: JSON.stringify({ code }),
  });
}

export function getCurrentUser(): Promise<AuthUser> {
  return apiFetch<AuthUser>("/auth/me");
}

export function logout(): Promise<void> {
  return apiFetchVoid("/auth/logout", { method: "POST" });
}

export function deleteAccount(password: string | null): Promise<void> {
  return apiFetchVoid("/auth/me", {
    method: "DELETE",
    body: JSON.stringify({ password }),
  });
}

export function requestPasswordReset(email: string): Promise<void> {
  return apiFetchVoid("/auth/password-reset/request", {
    method: "POST",
    body: JSON.stringify({ email }),
  });
}

export function resetPassword(token: string, newPassword: string): Promise<void> {
  return apiFetchVoid("/auth/password-reset/confirm", {
    method: "POST",
    body: JSON.stringify({ token, newPassword }),
  });
}

export function resendEmailVerification(): Promise<void> {
  return apiFetchVoid("/auth/email-verification/resend", { method: "POST" });
}

export function verifyEmail(token: string): Promise<void> {
  return apiFetchVoid("/auth/email-verification/confirm", {
    method: "POST",
    body: JSON.stringify({ token }),
  });
}

export async function establishSession(result: AuthResult): Promise<void> {
  useAuthStore.getState().setAccessToken(result.accessToken);
  const user = await getCurrentUser();
  useAuthStore.getState().setSession(result.accessToken, user);
}
