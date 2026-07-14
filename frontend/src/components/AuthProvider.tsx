"use client";

import type { ReactNode } from "react";
import { useEffect } from "react";
import { useLocale } from "next-intl";
import { useAuthStore } from "@/store/useAuthStore";
import { refreshSession } from "@/lib/apiClient";
import { getCurrentUser } from "@/lib/authApi";
import { getRefreshToken, clearRefreshToken } from "@/lib/refreshTokenStorage";
import { setCurrentLocale } from "@/lib/currentLocale";

export function AuthProvider({ children }: { children: ReactNode }) {
  const locale = useLocale();

  useEffect(() => {
    setCurrentLocale(locale);
  }, [locale]);

  useEffect(() => {
    const { setLoading, setSession, clearSession } = useAuthStore.getState();

    if (!getRefreshToken()) {
      clearSession();
      return;
    }

    setLoading();
    refreshSession()
      .then((accessToken) => getCurrentUser().then((user) => setSession(accessToken, user)))
      .catch(() => {
        clearRefreshToken();
        clearSession();
      });
  }, []);

  return <>{children}</>;
}
