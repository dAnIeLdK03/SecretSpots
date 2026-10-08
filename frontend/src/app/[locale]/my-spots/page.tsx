"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { useMySpotsListStore } from "@/store/useMySpotsListStore";
import { FeaturedSpotCard } from "@/components/FeaturedSpotCard";
import { Link } from "@/i18n/navigation";

export default function MySpotsPage() {
  const t = useTranslations("MySpots");
  const isAuthenticated = useRequireAuth();

  const items = useMySpotsListStore((state) => state.items);
  const status = useMySpotsListStore((state) => state.status);
  const totalCount = useMySpotsListStore((state) => state.totalCount);
  const loadFirstPage = useMySpotsListStore((state) => state.loadFirstPage);
  const loadMore = useMySpotsListStore((state) => state.loadMore);

  useEffect(() => {
    if (isAuthenticated) {
      loadFirstPage();
    }
  }, [isAuthenticated, loadFirstPage]);

  if (!isAuthenticated) {
    return null;
  }

  const hasMore = items.length < totalCount;

  return (
    <div className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-4 p-8">
      <h1 className="text-2xl font-semibold">{t("pageTitle")}</h1>

      {status === "loading" ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loading")}
        </p>
      ) : items.length === 0 ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("empty")}{" "}
          <Link href="/map" className="underline">
            {t("emptyAddLink")}
          </Link>
        </p>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 md:grid-cols-3">
          {items.map((spot) => (
            <FeaturedSpotCard key={spot.id} spot={spot} />
          ))}
        </div>
      )}

      {hasMore ? (
        <button
          onClick={() => loadMore()}
          disabled={status === "loadingMore"}
          className="self-center rounded border px-4 py-2 text-sm disabled:opacity-50"
          style={{ borderColor: "var(--fieldmap-contour)" }}
        >
          {status === "loadingMore" ? t("loadingMore") : t("loadMore")}
        </button>
      ) : null}
    </div>
  );
}
