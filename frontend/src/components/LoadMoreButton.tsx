"use client";

import { useTranslations } from "next-intl";
import type { PagedList } from "@/hooks/usePagedList";

// The "Load more" button under every paged list; renders nothing once everything is loaded.
export function LoadMoreButton({ list, className = "" }: { list: PagedList<{ id: string }>; className?: string }) {
  const t = useTranslations("Common");

  if (!list.hasMore) return null;

  return (
    <button
      onClick={list.loadMore}
      disabled={list.status !== "idle"}
      className={`rounded-lg border px-5 py-2 text-sm disabled:opacity-50 ${className}`}
      style={{ borderColor: "var(--fieldmap-contour)" }}
    >
      {list.status === "loadingMore" ? t("loadingMore") : t("loadMore")}
    </button>
  );
}
