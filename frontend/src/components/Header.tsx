"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Link, usePathname } from "@/i18n/navigation";
import { useAuthStore } from "@/store/useAuthStore";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { NotificationBell } from "@/components/NotificationBell";
import { AccountMenu } from "@/components/AccountMenu";

export function Header() {
  const t = useTranslations("Layout");
  const tHome = useTranslations("Home");
  const tAuth = useTranslations("Auth");
  const pathname = usePathname();
  const status = useAuthStore((state) => state.status);
  const user = useAuthStore((state) => state.user);

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

  function closeMenu() {
    setIsMenuOpen(false);
  }

  const navItems = [
    { href: "/", label: tHome("exploreNav") },
    { href: "/map", label: tHome("mapNav") },
    { href: "/saved", label: tHome("collectionsNav") },
    { href: "/about", label: tHome("aboutNav") },
  ];

  // The landing page renders its own hero header (LandingHero), and the auth
  // pages render a full-height split screen with no room for a navbar.
  if (
    pathname === "/" ||
    pathname === "/login" ||
    pathname === "/register" ||
    pathname === "/forgot-password" ||
    pathname === "/reset-password"
  ) {
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
        {navItems.map(({ href, label }) => (
          <Link
            key={href}
            href={href}
            className={pathname === href ? "border-b-2 pb-1" : "opacity-70 hover:opacity-100"}
            style={pathname === href ? { borderColor: "var(--fieldmap-trail)" } : undefined}
          >
            {label}
          </Link>
        ))}
      </nav>

      <div className="flex items-center gap-2">
        {status === "authenticated" && user ? (
          <>
            <NotificationBell />
            <AccountMenu displayName={user.displayName} mobileNavItems={navItems} />
          </>
        ) : (
          <>
            <LocaleSwitcher />

            <nav className="hidden items-center gap-4 text-sm sm:flex">
              <Link href="/login">{tAuth("loginTitle")}</Link>
              <Link href="/register">{tAuth("registerTitle")}</Link>
            </nav>

            <div ref={menuRef} className="relative sm:hidden">
              <button
                onClick={() => setIsMenuOpen((open) => !open)}
                aria-label={t("menuLabel")}
                className="rounded-full p-2 hover:bg-black/5 dark:hover:bg-white/5"
                style={{ color: "var(--fieldmap-dim)" }}
              >
                ☰
              </button>
              {isMenuOpen && (
                <div
                  className="absolute right-0 top-full z-10 mt-2 w-48 rounded-md border p-3 shadow-lg"
                  style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
                >
                  <nav className="flex flex-col gap-3 text-sm">
                    <Link href="/login" onClick={closeMenu}>
                      {tAuth("loginTitle")}
                    </Link>
                    <Link href="/register" onClick={closeMenu}>
                      {tAuth("registerTitle")}
                    </Link>
                  </nav>
                </div>
              )}
            </div>
          </>
        )}
      </div>
    </header>
  );
}
