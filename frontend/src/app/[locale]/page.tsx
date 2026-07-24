"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { LandingHero } from "@/components/LandingHero";
import { FeaturedSpotCard } from "@/components/FeaturedSpotCard";
import { getNearbySpots, searchSpots, SPOT_CATEGORIES } from "@/lib/spotsApi";
import type { NearbySpot, SpotCategory, SpotSearchResult } from "@/lib/spotsApi";

const SOFIA_CENTER = { lat: 42.6977, lng: 23.3219 };
const PAGE_SIZE = 4;

export default function LandingPage() {
  const t = useTranslations("Home");
  const tSpots = useTranslations("Spots");
  const [spots, setSpots] = useState<NearbySpot[]>([]);
  const [categoryFilter, setCategoryFilter] = useState<SpotCategory | "All">("All");
  const [visibleCount, setVisibleCount] = useState(PAGE_SIZE);
  const [searchResults, setSearchResults] = useState<SpotSearchResult[] | null>(null);
  const [searchTerm, setSearchTerm] = useState<string | null>(null);

  useEffect(() => {
    getNearbySpots(SOFIA_CENTER.lat, SOFIA_CENTER.lng, 50)
      .then(setSpots)
      .catch(() => { });
  }, []);

  function handleCategoryChange(category: SpotCategory | "All") {
    setCategoryFilter(category);
    setVisibleCount(PAGE_SIZE);
    setSearchResults(null);
    setSearchTerm(null);
  }

  const handleSearch = useCallback((term: string) => {
    const trimmed = term.trim();
    if (!trimmed){
      setSearchResults(null);
      setSearchTerm(null);
      return;
    }

    const matchedCategory = SPOT_CATEGORIES.find(
      (category) => tSpots(`category.${category}`).toLowerCase() === trimmed.toLowerCase(),
    );

    if (matchedCategory) {
      handleCategoryChange(matchedCategory);
      return;
    }

    setSearchTerm(trimmed);
    searchSpots({ q: trimmed })
      .then(setSearchResults)
      .catch(() => setSearchResults([]));
  },
  [tSpots]
);

  const filteredSpots = categoryFilter === "All" ? spots : spots.filter((s) => s.category === categoryFilter);
  const displayedSpots = searchResults !== null ? searchResults : filteredSpots;
  const visibleSpots = displayedSpots.slice(0, visibleCount);

  return (
    <div className="flex-1">
      <LandingHero onSearch={handleSearch} />

      <div className="mx-auto max-w-6xl px-6 py-10">
        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => handleCategoryChange("All")}
            className={`rounded-full px-4 py-2 text-sm ${categoryFilter === "All"
              ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900"
              : "bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
              }`}
          >
            {t("allSpotsFilter")}
          </button>
          {SPOT_CATEGORIES.map((category) => (
            <button
              key={category}
              onClick={() => handleCategoryChange(category)}
              className={`rounded-full px-4 py-2 text-sm ${categoryFilter === category
                ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900"
                : "bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
                }`}
            >
              {tSpots(`category.${category}`)}
            </button>
          ))}
        </div>

        <div className="mt-8 flex flex-wrap items-end justify-between gap-4">
          <div>
            <h2 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-100">
              {searchResults !== null ? t("searchResultsTitle", { term: searchTerm ?? "" }) : t("featuredTitle")}
            </h2>
            <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
              {searchResults !== null ? t("searchResultsSubtitle") : t("featuredSubtitle")}
            </p>
          </div>
          <Link
            href="/map"
            className="rounded bg-zinc-900 px-4 py-2 text-sm text-white dark:bg-zinc-100 dark:text-zinc-900"
          >
            {t("exploreMapButton")}
          </Link>
        </div>

        {visibleSpots.length === 0 ? (
          <p className="mt-8 text-sm text-zinc-500">
            {searchResults !== null ? t("noSearchResults") : t("noSpotsYet")}
          </p>
        ) : (
          <div className="mt-6 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {visibleSpots.map((spot) => (
              <FeaturedSpotCard key={spot.id} spot={spot} />
            ))}
          </div>
        )}

        {visibleCount < displayedSpots.length ? (
          <div className="mt-8 flex justify-center">
            <button
              onClick={() => setVisibleCount((count) => count + PAGE_SIZE)}
              className="rounded border border-zinc-300 px-4 py-2 text-sm dark:border-zinc-700"
            >
              {t("loadMoreSpots")}
            </button>
          </div>
        ) : null}
      </div>

      <div className="bg-zinc-900 px-6 py-10 text-white dark:bg-black">
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-4 sm:flex-row">
          <div className="text-center sm:text-left">
            <h3 className="text-lg font-semibold">{t("ctaTitle")}</h3>
            <p className="text-sm text-zinc-400">{t("ctaSubtitle")}</p>
          </div>
          <Link href="/map" className="rounded-full bg-emerald-500 px-5 py-3 text-sm font-medium whitespace-nowrap">
            {t("addASpot")}
          </Link>
        </div>
      </div>
    </div>
  );
}
