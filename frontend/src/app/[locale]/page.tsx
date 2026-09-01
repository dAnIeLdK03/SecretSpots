"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { LandingHero } from "@/components/LandingHero";
import { FeaturedSpotCard } from "@/components/FeaturedSpotCard";
import { searchSpots, SPOT_CATEGORIES } from "@/lib/spotsApi";
import type { SpotCategory, SpotSearchResult } from "@/lib/spotsApi";
import { CategoryIcon } from "@/components/CategoryIcon";

const PAGE_SIZE = 4;

export default function LandingPage() {
  const t = useTranslations("Home");
  const tSpots = useTranslations("Spots");
  const [categoryFilter, setCategoryFilter] = useState<SpotCategory | "All">("All");
  const [searchTerm, setSearchTerm] = useState<string | null>(null);
  const [items, setItems] = useState<SpotSearchResult[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loadingMore, setLoadingMore] = useState(false);

  const fetchPage = useCallback(
    (pageNum: number, append: boolean, signal?: AbortSignal) => {
      const category = categoryFilter === "All" ? undefined : categoryFilter;
      const q = searchTerm ?? undefined;
      return searchSpots({ q, category, page: pageNum, pageSize: PAGE_SIZE }, signal).then((result) => {
        setItems((prev) => (append ? [...prev, ...result.items] : result.items));
        setTotalCount(result.totalCount);
        setPage(pageNum);
      });
    },
    [categoryFilter, searchTerm],
  );

  useEffect(() => {
    const controller = new AbortController();
    fetchPage(1, false, controller.signal).catch(() => {
      if (controller.signal.aborted) return;
      setItems([]);
      setTotalCount(0);
    });
    return () => controller.abort();
  }, [fetchPage]);

  function handleCategoryChange(category: SpotCategory | "All") {
    setCategoryFilter(category);
    setSearchTerm(null);
  }

  const handleSearch = useCallback(
    (term: string) => {
      const trimmed = term.trim();
      if (!trimmed) {
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
    },
    [tSpots],
  );

  async function handleLoadMore() {
    setLoadingMore(true);
    try {
      await fetchPage(page + 1, true);
    } finally {
      setLoadingMore(false);
    }
  }

  const isSearching = searchTerm !== null;

  // Fuzzy/word-level matching on the backend means a result can show up without
  // literally containing the search term — call that out so it doesn't read as a bug.
  const hasExactMatch =
    !isSearching ||
    items.length === 0 ||
    items.some(
      (s) =>
        s.name.toLowerCase().includes(searchTerm!.toLowerCase()) ||
        s.description.toLowerCase().includes(searchTerm!.toLowerCase()),
    );

  return (
    <div className="flex-1" style={{ backgroundColor: "var(--fieldmap-paper)", color: "var(--fieldmap-ink)" }}>
      <LandingHero onSearch={handleSearch} />

      <div className="mx-auto max-w-6xl px-6 py-10">
        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => handleCategoryChange("All")}
            className="rounded-full px-4 py-2 text-sm font-medium"
            style={
              categoryFilter === "All"
                ? { backgroundColor: "var(--fieldmap-trail)", color: "#f1eddc" }
                : { backgroundColor: "var(--fieldmap-card)", color: "var(--fieldmap-ink)" }
            }
          >
            {t("allSpotsFilter")}
          </button>
          {SPOT_CATEGORIES.map((category) => (
            <button
              key={category}
              onClick={() => handleCategoryChange(category)}
              className="flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-medium"
              style={
                categoryFilter === category
                  ? { backgroundColor: "var(--fieldmap-trail)", color: "#f1eddc" }
                  : { backgroundColor: "var(--fieldmap-card)", color: "var(--fieldmap-ink)" }
              }
            >
              <CategoryIcon category={category} size={14} />
              {tSpots(`category.${category}`)}
            </button>
          ))}
        </div>

        <div className="mt-8 flex flex-wrap items-end justify-between gap-4">
          <div>
            <h2 className="text-2xl font-semibold">
              {isSearching ? t("searchResultsTitle", { term: searchTerm ?? "" }) : t("featuredTitle")}
            </h2>
            <p className="mt-1 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
              {isSearching
                ? hasExactMatch
                  ? t("searchResultsSubtitle")
                  : t("approximateResultsSubtitle", { term: searchTerm ?? "" })
                : t("featuredSubtitle")}
            </p>
          </div>
          <Link
            href="/map"
            className="rounded px-4 py-2 text-sm font-medium"
            style={{ backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper)" }}
          >
            {t("exploreMapButton")}
          </Link>
        </div>

        {items.length === 0 ? (
          <p className="mt-8 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
            {isSearching ? t("noSearchResults") : t("noSpotsYet")}
          </p>
        ) : (
          <div className="mt-6 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {items.map((spot) => (
              <FeaturedSpotCard key={spot.id} spot={spot} />
            ))}
          </div>
        )}

        {items.length < totalCount ? (
          <div className="mt-8 flex justify-center">
            <button
              onClick={handleLoadMore}
              disabled={loadingMore}
              className="rounded border px-4 py-2 text-sm disabled:opacity-50"
              style={{ borderColor: "var(--fieldmap-contour)" }}
            >
              {t("loadMoreSpots")}
            </button>
          </div>
        ) : null}
      </div>

      <div className="px-6 py-10" style={{ backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper)" }}>
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-4 sm:flex-row">
          <div className="text-center sm:text-left">
            <h3 className="text-lg font-semibold">{t("ctaTitle")}</h3>
            <p className="text-sm opacity-70">{t("ctaSubtitle")}</p>
          </div>
          <Link
            href="/map"
            className="rounded-full px-5 py-3 text-sm font-medium whitespace-nowrap"
            style={{ backgroundColor: "var(--fieldmap-trail)", color: "#f1eddc" }}
          >
            {t("addASpot")}
          </Link>
        </div>
      </div>
    </div>
  );
}
