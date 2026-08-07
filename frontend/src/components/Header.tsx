"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Link, usePathname } from "@/i18n/navigation";
import { useAuthStore } from "@/store/useAuthStore";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { useCheckInsHistoryStore } from "@/store/useCheckInsHistoryStore";
import { getRefreshToken, clearRefreshToken } from "@/lib/refreshTokenStorage";
import { logout } from "@/lib/authApi";
import { NotificationBell } from "@/components/NotificationBell";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";

export function Header() {
  const t = useTranslations("Layout");
  const tHome = useTranslations("Home");
  const tAuth = useTranslations("Auth");
  const pathname = usePathname();
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
        <Link href="/account" onClick={closeMenu} style={{ color: "var(--fieldmap-dim)" }}>
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

  // The landing page renders its own hero header (LandingHero), and the auth
  // pages render a full-height split screen with no room for a navbar.
  if (pathname === "/" || pathname === "/login" || pathname === "/register") {
    return null;
  }

  return (
    <header
      className="flex items-center justify-between border-b px-6 py-4"
      style={{ borderColor: "var(--fieldmap-contour)" }}
    >
      <Link href="/" className="text-lg font-semibold">
        {t("appName")}
      </Link>

      <nav className="hidden items-center gap-6 text-sm sm:flex">
        <Link
          href="/"
          className={pathname === "/" ? "border-b-2 pb-1" : "opacity-70 hover:opacity-100"}
          style={pathname === "/" ? { borderColor: "var(--fieldmap-trail)" } : undefined}
        >
          {tHome("exploreNav")}
        </Link>
        <Link
          href="/map"
          className={pathname === "/map" ? "border-b-2 pb-1" : "opacity-70 hover:opacity-100"}
          style={pathname === "/map" ? { borderColor: "var(--fieldmap-trail)" } : undefined}
        >
          {tHome("mapNav")}
        </Link>
        <Link
          href="/saved"
          className={pathname === "/saved" ? "border-b-2 pb-1" : "opacity-70 hover:opacity-100"}
          style={pathname === "/saved" ? { borderColor: "var(--fieldmap-trail)" } : undefined}
        >
          {tHome("collectionsNav")}
        </Link>
        <span className="cursor-default opacity-40">{tHome("aboutNav")}</span>
      </nav>

      <div className="flex items-center gap-2">
        <LocaleSwitcher />

        {status === "authenticated" && user && <NotificationBell />}

        <nav className="hidden items-center gap-4 text-sm sm:flex">{navLinks}</nav>

        <div ref={menuRef} className="relative sm:hidden">
          <button
            onClick={() => setIsMenuOpen((open) => !open)}
            aria-label={t("menuLabel")}
            className="rounded-full p-2 hover:bg-black/5"
            style={{ color: "var(--fieldmap-dim)" }}
          >
            ☰
          </button>
          {isMenuOpen && (
            <div
              className="absolute right-0 top-full z-10 mt-2 w-48 rounded-md border p-3 shadow-lg"
              style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
            >
              <nav className="flex flex-col gap-3 text-sm">{navLinks}</nav>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
