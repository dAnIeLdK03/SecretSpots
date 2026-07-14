"use client";

import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useAuthStore } from "@/store/useAuthStore";
import { getRefreshToken, clearRefreshToken } from "@/lib/refreshTokenStorage";
import { logout } from "@/lib/authApi";

export function Header() {
  const t = useTranslations("Layout");
  const tAuth = useTranslations("Auth");
  const status = useAuthStore((state) => state.status);
  const user = useAuthStore((state) => state.user);
  const clearSession = useAuthStore((state) => state.clearSession);

  function handleLogout() {
    const refreshToken = getRefreshToken();
    clearRefreshToken();
    clearSession();
    if (refreshToken) {
      logout(refreshToken).catch(() => {});
    }
  }

  return (
    <header className="flex items-center justify-between border-b border-zinc-200 px-6 py-4 dark:border-zinc-800">
      <Link href="/" className="text-lg font-semibold">
        {t("appName")}
      </Link>
      <nav className="flex items-center gap-4 text-sm">
        {status === "authenticated" && user ? (
          <>
            <Link href="/account" className="text-zinc-600 dark:text-zinc-400">
              {user.displayName} ({user.crystalBalance} {tAuth("crystalBalanceLabel")})
            </Link>
            <button onClick={handleLogout} className="underline">
              {tAuth("logoutButton")}
            </button>
          </>
        ) : (
          <>
            <Link href="/login">{tAuth("loginTitle")}</Link>
            <Link href="/register">{tAuth("registerTitle")}</Link>
          </>
        )}
      </nav>
    </header>
  );
}
