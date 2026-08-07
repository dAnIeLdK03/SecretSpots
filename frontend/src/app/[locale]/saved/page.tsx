"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { useSavedSpotsListStore } from "@/store/useSavedSpotsListStore";
import { SavedSpotCard } from "@/components/SavedSpotCard";

export default function SavedSpotsPage() {
  const t = useTranslations("SavedSpots");
  const isAuthenticated = useRequireAuth();

  const items = useSavedSpotsListStore((state) => state.items);
  const status = useSavedSpotsListStore((state) => state.status);
  const totalCount = useSavedSpotsListStore((state) => state.totalCount);
  const loadFirstPage = useSavedSpotsListStore((state) => state.loadFirstPage);
  const loadMore = useSavedSpotsListStore((state) => state.loadMore);

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
          {t("empty")}
        </p>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 md:grid-cols-3">
          {items.map((spot) => (
            <SavedSpotCard key={spot.spotId} spot={spot} />
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
