"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useAuthStore } from "@/store/useAuthStore";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { useCheckInsHistoryStore } from "@/store/useCheckInsHistoryStore";
import { getRefreshToken, clearRefreshToken } from "@/lib/refreshTokenStorage";
import { logout } from "@/lib/authApi";
import { NotificationBell } from "@/components/NotificationBell";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";

export function Header() {
  const t = useTranslations("Layout");
  const tAuth = useTranslations("Auth");
  const status = useAuthStore((state) => state.status);
  const user = useAuthStore((state) => state.user);
  const clearSession = useAuthStore((state) => state.clearSession);
  const resetNotifications = useNotificationsStore((state) => state.reset);
  const resetCheckInsHistory = useCheckInsHistoryStore((state) => state.reset);

  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isMenuOpen) return;

    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isMenuOpen]);

  function handleLogout() {
    const refreshToken = getRefreshToken();
    clearRefreshToken();
    clearSession();
    resetNotifications();
    resetCheckInsHistory();
    if (refreshToken) {
      logout(refreshToken).catch(() => {});
    }
  }

  function closeMenu() {
    setIsMenuOpen(false);
  }

  const navLinks =
    status === "authenticated" && user ? (
      <>
        <Link href="/account" onClick={closeMenu} className="text-zinc-600 dark:text-zinc-400">
          {user.displayName} ({user.crystalBalance} {tAuth("crystalBalanceLabel")})
        </Link>
        <button
          onClick={() => {
            handleLogout();
            closeMenu();
          }}
          className="text-left underline"
        >
          {tAuth("logoutButton")}
        </button>
      </>
    ) : (
      <>
        <Link href="/login" onClick={closeMenu}>
          {tAuth("loginTitle")}
        </Link>
        <Link href="/register" onClick={closeMenu}>
          {tAuth("registerTitle")}
        </Link>
      </>
    );

  return (
    <header className="flex items-center justify-between border-b border-zinc-200 px-6 py-4 dark:border-zinc-800">
      <Link href="/" className="text-lg font-semibold">
        {t("appName")}
      </Link>
      <div className="flex items-center gap-2">
        <LocaleSwitcher />

        {status === "authenticated" && user && <NotificationBell />}

        <nav className="hidden items-center gap-4 text-sm sm:flex">{navLinks}</nav>

        <div ref={menuRef} className="relative sm:hidden">
          <button
            onClick={() => setIsMenuOpen((open) => !open)}
            aria-label={t("menuLabel")}
            className="rounded-full p-2 text-zinc-600 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800"
          >
            ☰
          </button>
          {isMenuOpen && (
            <div className="absolute right-0 top-full z-10 mt-2 w-48 rounded-md border border-zinc-200 bg-white p-3 shadow-lg dark:border-zinc-800 dark:bg-zinc-900">
              <nav className="flex flex-col gap-3 text-sm">{navLinks}</nav>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
