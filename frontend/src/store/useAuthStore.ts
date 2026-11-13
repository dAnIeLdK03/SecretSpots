import { create } from "zustand";
import type { UserRole } from "@/lib/userRole";

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  isEmailVerified: boolean;
  role: UserRole;
}

export type AuthStatus = "idle" | "loading" | "authenticated" | "unauthenticated";

interface AuthStore {
  accessToken: string | null;
  user: AuthUser | null;
  status: AuthStatus;
  setLoading: () => void;
  setSession: (accessToken: string, user: AuthUser) => void;
  setAccessToken: (accessToken: string) => void;
  clearSession: () => void;
}

export const useAuthStore = create<AuthStore>((set) => ({
  accessToken: null,
  user: null,
  status: "idle",
  setLoading: () => set({ status: "loading" }),
  setSession: (accessToken, user) => set({ accessToken, user, status: "authenticated" }),
  setAccessToken: (accessToken) => set({ accessToken }),
  clearSession: () => set({ accessToken: null, user: null, status: "unauthenticated" }),
}));
