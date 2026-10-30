"use client";

import { useTranslations } from "next-intl";
import dynamic from "next/dynamic";
import { Link } from "@/i18n/navigation";
import { useAuthStore } from "@/store/useAuthStore";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { NotificationBell } from "@/components/NotificationBell";
import { AccountMenu } from "@/components/AccountMenu";
import { HeroContourBackground } from "@/components/HeroContourBackground";
import { useEffect, useState } from "react";

// MapLibre is a large dependency and touches window/canvas, so it's kept out of the landing
// page's initial bundle — see the same dynamic import on the /map page for why.
const HeroMap = dynamic(() => import("@/components/HeroMap").then((m) => m.HeroMap), { ssr: false });

interface LandingHeroProps {
  onSearch: (term: string) => void;
}

export function LandingHero({ onSearch }: LandingHeroProps) {
  const t = useTranslations("Home");
  const tAuth = useTranslations("Auth");
  const status = useAuthStore((state) => state.status);
  const user = useAuthStore((state) => state.user);

  const [searchTerm, setSearchTerm] = useState("");

  useEffect(() => {
    const timeoutId = setTimeout(() => onSearch(searchTerm), 400);
    return () => clearTimeout(timeoutId);
  }, [searchTerm, onSearch]);

  return (
    <div className="relative overflow-hidden bg-paper text-ink">
      <header className="relative z-10 flex items-center justify-between gap-4 px-6 py-4">
        <div className="flex items-center gap-2 text-lg font-semibold tracking-tight">SecretSpots</div>

        <nav className="hidden items-center gap-6 text-sm sm:flex">
          <Link href="/" className="border-b-2 border-trail pb-1">
            {t("exploreNav")}
          </Link>
          <Link href="/map" className="opacity-70 hover:opacity-100">
            {t("mapNav")}
          </Link>
          <Link href="/saved" className="opacity-70 hover:opacity-100">
            {t("collectionsNav")}
          </Link>
          <Link href="/about" className="opacity-70 hover:opacity-100">
            {t("aboutNav")}
          </Link>
        </nav>

        <div className="flex items-center gap-3">
          {status === "authenticated" && user ? (
            <>
              <NotificationBell />
              <AccountMenu
                displayName={user.displayName}
                mobileNavItems={[
                  { href: "/", label: t("exploreNav") },
                  { href: "/map", label: t("mapNav") },
                  { href: "/saved", label: t("collectionsNav") },
                  { href: "/about", label: t("aboutNav") },
                ]}
              />
            </>
          ) : (
            <>
              <LocaleSwitcher />
              <Link href="/login" className="text-sm opacity-70 hover:opacity-100">
                {tAuth("loginTitle")}
              </Link>
              <Link
                href="/register"
                className="rounded-full border border-ink px-4 py-2 text-sm whitespace-nowrap hover:bg-black/5 dark:hover:bg-white/5"
              >
                {tAuth("registerTitle")}
              </Link>
            </>
          )}
        </div>
      </header>

      <div className="relative z-10 mx-auto grid max-w-6xl gap-8 px-6 pt-8 pb-16 sm:grid-cols-2 sm:items-center">
        <div className="relative">
          <HeroContourBackground />
          <div className="relative">
            <h1 className="[transform-origin:left] [transform:scaleX(0.94)] text-4xl leading-tight font-extrabold uppercase sm:text-5xl">
              {t("heroTitlePrefix")}
              <span className="text-trail">{t("heroTitleAccent")}</span>
            </h1>
            <p className="mt-4 max-w-md text-dim">{t("heroSubtitle")}</p>

            <form
              onSubmit={(e) => {
                e.preventDefault();
                onSearch(searchTerm);
              }}
              className="mt-6 flex items-center gap-2"
            >
              <input
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder={t("searchPlaceholder")}
                aria-label={t("searchPlaceholder")}
                className="w-full rounded-full px-4 py-3 text-sm placeholder:opacity-60"
                style={{ backgroundColor: "#f1eddc", color: "#2b2a23" }}
              />
              <button
                type="submit"
                aria-label={t("searchButtonLabel")}
                className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-trail text-on-trail"
              >
                →
              </button>
            </form>
          </div>
        </div>

        <div className="h-64 sm:h-80">
          <HeroMap />
        </div>
      </div>
    </div>
  );
}
