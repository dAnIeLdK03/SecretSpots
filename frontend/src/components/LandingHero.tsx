"use client";

import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useAuthStore } from "@/store/useAuthStore";
import { NotificationBell } from "@/components/NotificationBell";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { HeroMap } from "@/components/HeroMap";

export function LandingHero() {
  const t = useTranslations("Home");
  const tAuth = useTranslations("Auth");
  const status = useAuthStore((state) => state.status);
  const user = useAuthStore((state) => state.user);

  return (
    <div className="relative overflow-hidden bg-gradient-to-br from-zinc-900 via-emerald-950 to-zinc-950 text-white">
      <header className="relative z-10 flex items-center justify-between gap-4 px-6 py-4">
        <div className="flex items-center gap-2 text-lg font-semibold">
          <span aria-hidden="true">📍</span>
          SecretSpots
        </div>

        <nav className="hidden items-center gap-6 text-sm sm:flex">
          <Link href="/" className="border-b-2 border-emerald-400 pb-1">
            {t("exploreNav")}
          </Link>
          <Link href="/map" className="text-zinc-300 hover:text-white">
            {t("mapNav")}
          </Link>
          <span className="cursor-default text-zinc-500">{t("collectionsNav")}</span>
          <span className="cursor-default text-zinc-500">{t("aboutNav")}</span>
        </nav>

        <div className="flex items-center gap-3">
          <LocaleSwitcher />
          {status === "authenticated" && user ? (
            <>
              <NotificationBell />
              <Link
                href="/map"
                className="rounded-full border border-white/30 px-4 py-2 text-sm whitespace-nowrap hover:bg-white/10"
              >
                {t("addASpot")}
              </Link>
              <Link
                href="/account"
                aria-label={user.displayName}
                className="flex h-8 w-8 items-center justify-center rounded-full bg-white/20 text-sm font-semibold"
              >
                {user.displayName.charAt(0).toUpperCase()}
              </Link>
            </>
          ) : (
            <>
              <Link href="/login" className="text-sm text-zinc-300 hover:text-white">
                {tAuth("loginTitle")}
              </Link>
              <Link
                href="/register"
                className="rounded-full border border-white/30 px-4 py-2 text-sm whitespace-nowrap hover:bg-white/10"
              >
                {tAuth("registerTitle")}
              </Link>
            </>
          )}
        </div>
      </header>

      <div className="relative z-10 mx-auto grid max-w-6xl gap-8 px-6 pt-8 pb-16 sm:grid-cols-2 sm:items-center">
        <div>
          <h1 className="text-4xl font-bold sm:text-5xl">
            {t("heroTitlePrefix")}
            <span className="text-emerald-400">{t("heroTitleAccent")}</span>
          </h1>
          <p className="mt-4 max-w-md text-zinc-300">{t("heroSubtitle")}</p>

          <div className="mt-6 flex items-center gap-2">
            <input
              type="text"
              placeholder={t("searchPlaceholder")}
              className="w-full rounded-full bg-white px-4 py-3 text-sm text-zinc-900 placeholder:text-zinc-500"
            />
            <Link
              href="/map"
              aria-label={t("mapNav")}
              className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-emerald-500 text-white"
            >
              📍
            </Link>
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-2 text-sm text-zinc-400">
            <span>{t("popularSearchesLabel")}:</span>
            {[t("popularSearchTag1"), t("popularSearchTag2"), t("popularSearchTag3"), t("popularSearchTag4")].map(
              (tag) => (
                <span key={tag} className="rounded-full border border-white/20 px-3 py-1">
                  {tag}
                </span>
              ),
            )}
          </div>
        </div>

        <div className="h-64 sm:h-80">
          <HeroMap />
        </div>
      </div>
    </div>
  );
}
