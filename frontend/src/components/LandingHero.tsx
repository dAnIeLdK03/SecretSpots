"use client";

import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useAuthStore } from "@/store/useAuthStore";
import { NotificationBell } from "@/components/NotificationBell";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { HeroMap } from "@/components/HeroMap";
import { HeroContourBackground } from "@/components/HeroContourBackground";
import { useEffect, useState } from "react";

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
    <div
      className="relative overflow-hidden"
      style={{ backgroundColor: "var(--fieldmap-paper)", color: "var(--fieldmap-ink)" }}
    >
      <header className="relative z-10 flex items-center justify-between gap-4 px-6 py-4">
        <div className="flex items-center gap-2 text-lg font-semibold tracking-tight">
          SecretSpots
        </div>

        <nav className="hidden items-center gap-6 text-sm sm:flex">
          <Link href="/" className="border-b-2 pb-1" style={{ borderColor: "var(--fieldmap-trail)" }}>
            {t("exploreNav")}
          </Link>
          <Link href="/map" className="opacity-70 hover:opacity-100">
            {t("mapNav")}
          </Link>
          <span className="cursor-default opacity-40">{t("collectionsNav")}</span>
          <span className="cursor-default opacity-40">{t("aboutNav")}</span>
        </nav>

        <div className="flex items-center gap-3">
          <LocaleSwitcher />
          {status === "authenticated" && user ? (
            <>
              <NotificationBell />
              <Link
                href="/map"
                className="rounded-full border px-4 py-2 text-sm whitespace-nowrap hover:bg-black/5"
                style={{ borderColor: "var(--fieldmap-ink)" }}
              >
                {t("addASpot")}
              </Link>
              <Link
                href="/account"
                aria-label={user.displayName}
                className="flex h-8 w-8 items-center justify-center rounded-full text-sm font-semibold"
                style={{ backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper)" }}
              >
                {user.displayName.charAt(0).toUpperCase()}
              </Link>
            </>
          ) : (
            <>
              <Link href="/login" className="text-sm opacity-70 hover:opacity-100">
                {tAuth("loginTitle")}
              </Link>
              <Link
                href="/register"
                className="rounded-full border px-4 py-2 text-sm whitespace-nowrap hover:bg-black/5"
                style={{ borderColor: "var(--fieldmap-ink)" }}
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
            <h1
              className="text-4xl leading-tight font-extrabold uppercase [transform:scaleX(0.94)] [transform-origin:left] sm:text-5xl"
            >
              {t("heroTitlePrefix")}
              <span style={{ color: "var(--fieldmap-trail)" }}>{t("heroTitleAccent")}</span>
            </h1>
            <p className="mt-4 max-w-md" style={{ color: "var(--fieldmap-dim)" }}>
              {t("heroSubtitle")}
            </p>

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
                className="w-full rounded-full px-4 py-3 text-sm placeholder:opacity-60"
                style={{ backgroundColor: "#f1eddc", color: "var(--fieldmap-ink)" }}
              />
              <button
                type="submit"
                aria-label={t("searchButtonLabel")}
                className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full"
                style={{ backgroundColor: "var(--fieldmap-trail)", color: "#f1eddc" }}
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
