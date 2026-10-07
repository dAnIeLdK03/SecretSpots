"use client";

import type { ReactNode } from "react";
import { useEffect } from "react";
import { useLocale } from "next-intl";
import { useAuthStore } from "@/store/useAuthStore";
import { useGeolocationStore } from "@/store/useGeolocationStore";
import { refreshSession } from "@/lib/apiClient";
import { getCurrentUser } from "@/lib/authApi";
import { setCurrentLocale } from "@/lib/currentLocale";

// Mirrors RefreshTokenCookie.SessionMarkerName on the backend — not HttpOnly, carries no secret,
// just lets us skip the refresh call (and its guaranteed 401) when there's no session at all.
const SESSION_MARKER_COOKIE = "secretspots_has_session";

function hasSessionMarker(): boolean {
  return document.cookie.split("; ").some((entry) => entry.startsWith(`${SESSION_MARKER_COOKIE}=`));
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const locale = useLocale();

  useEffect(() => {
    setCurrentLocale(locale);
  }, [locale]);

  // Kicked off here, as early as the app mounts, rather than on the map page —
  // by the time the user navigates to /map the position is usually already resolved.
  useEffect(() => {
    useGeolocationStore.getState().requestLocation();
  }, []);

  useEffect(() => {
    const { setLoading, setSession, clearSession } = useAuthStore.getState();

    if (!hasSessionMarker()) {
      clearSession();
      return;
    }

    setLoading();
    refreshSession()
      .then((accessToken) => getCurrentUser().then((user) => setSession(accessToken, user)))
      .catch(() => {
        clearSession();
      });
  }, []);

  return <>{children}</>;
}
