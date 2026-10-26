"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { getErrorMessage } from "@/lib/apiClient";
import { getAdminSpots } from "@/lib/adminApi";
import type { SpotSearchResult } from "@/lib/spotsApi";
import { ModerationStatusBadge } from "@/components/ModerationStatusBadge";

const PAGE_SIZE = 20;
// How close (in px) to the right edge the scroll position has to get before the next page loads.
const LOAD_MORE_THRESHOLD_PX = 200;

function SpotTile({ spot }: { spot: SpotSearchResult }) {
  const [imageFailed, setImageFailed] = useState(false);

  return (
    <Link
      href={`/spots/${spot.id}`}
      className="relative block aspect-square w-40 shrink-0 snap-start overflow-hidden rounded-xl sm:w-48"
      style={{ backgroundColor: "var(--fieldmap-contour)" }}
    >
      {!imageFailed ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          src={spot.photoUrl}
          alt={spot.name}
          onError={() => setImageFailed(true)}
          className="h-full w-full object-cover transition hover:scale-105"
        />
      ) : null}
      <ModerationStatusBadge status={spot.moderationStatus} className="absolute top-2 left-2" />
      <span className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/75 to-transparent px-2 pt-6 pb-2 text-sm font-medium break-words text-white">
        {spot.name}
      </span>
    </Link>
  );
}

export function AdminUserSpotsCarousel({ userId }: { userId: string }) {
  const t = useTranslations("AdminUserDetails");
  const scrollerRef = useRef<HTMLDivElement>(null);
  const [items, setItems] = useState<SpotSearchResult[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<"loading" | "idle" | "loadingMore">("loading");
  const [error, setError] = useState<string | null>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);

  const hasMore = items.length < totalCount;

  const updateScrollButtons = useCallback(() => {
    const el = scrollerRef.current;
    if (!el) return;
    setCanScrollLeft(el.scrollLeft > 0);
    setCanScrollRight(el.scrollLeft + el.clientWidth < el.scrollWidth - 1);
  }, []);

  useEffect(() => {
    const controller = new AbortController();

    getAdminSpots(1, PAGE_SIZE, { userId }, controller.signal)
      .then((result) => {
        setItems(result.items);
        setTotalCount(result.totalCount);
        setPage(1);
        setError(null);
      })
      .catch((err) => {
        if (controller.signal.aborted) return;
        setError(getErrorMessage(err, t("unknownError")));
      })
      .finally(() => {
        if (!controller.signal.aborted) setStatus("idle");
      });

    return () => controller.abort();
  }, [userId, t]);

  // Recompute after every render that changes the number of tiles — new tiles change scrollWidth.
  useEffect(() => {
    updateScrollButtons();
  }, [items.length, updateScrollButtons]);

  async function loadMore() {
    setStatus("loadingMore");
    try {
      const result = await getAdminSpots(page + 1, PAGE_SIZE, { userId });
      setItems((prev) => [...prev, ...result.items]);
      setTotalCount(result.totalCount);
      setPage(result.page);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setStatus("idle");
    }
  }

  function handleScroll() {
    updateScrollButtons();
    const el = scrollerRef.current;
    if (!el || !hasMore || status !== "idle") return;
    if (el.scrollLeft + el.clientWidth >= el.scrollWidth - LOAD_MORE_THRESHOLD_PX) {
      void loadMore();
    }
  }

  function scrollByPage(direction: 1 | -1) {
    const el = scrollerRef.current;
    if (!el) return;
    el.scrollBy({ left: direction * el.clientWidth * 0.8, behavior: "smooth" });
  }

  return (
    <section className="flex flex-col gap-3 rounded-2xl p-4" style={{ backgroundColor: "var(--fieldmap-card)" }}>
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-sm font-semibold">
          {t("spotsHeading")}
          {status !== "loading" && !error ? (
            <span className="ml-1 font-normal" style={{ color: "var(--fieldmap-dim)" }}>
              ({totalCount})
            </span>
          ) : null}
        </h2>
        {items.length > 0 ? (
          <div className="flex gap-1">
            <button
              onClick={() => scrollByPage(-1)}
              disabled={!canScrollLeft}
              aria-label={t("previousSpots")}
              className="rounded-full px-2 py-1 text-lg leading-none hover:bg-black/5 disabled:opacity-30 dark:hover:bg-white/5"
            >
              ←
            </button>
            <button
              onClick={() => scrollByPage(1)}
              disabled={!canScrollRight}
              aria-label={t("nextSpots")}
              className="rounded-full px-2 py-1 text-lg leading-none hover:bg-black/5 disabled:opacity-30 dark:hover:bg-white/5"
            >
              →
            </button>
          </div>
        ) : null}
      </div>

      {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}

      {status === "loading" ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loading")}
        </p>
      ) : items.length === 0 && !error ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("spotsEmpty")}
        </p>
      ) : (
        <div ref={scrollerRef} onScroll={handleScroll} className="flex snap-x snap-mandatory gap-3 overflow-x-auto pb-2">
          {items.map((spot) => (
            <SpotTile key={spot.id} spot={spot} />
          ))}
          {status === "loadingMore" ? (
            <div
              className="flex aspect-square w-40 shrink-0 items-center justify-center rounded-xl text-sm sm:w-48"
              style={{ backgroundColor: "var(--fieldmap-contour)", color: "var(--fieldmap-dim)" }}
            >
              {t("loading")}
            </div>
          ) : null}
        </div>
      )}
    </section>
  );
}
