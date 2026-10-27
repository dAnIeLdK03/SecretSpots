"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { usePagedList } from "@/hooks/usePagedList";
import { useAuthStore } from "@/store/useAuthStore";
import { FeaturedSpotCard } from "@/components/FeaturedSpotCard";
import { LoadMoreButton } from "@/components/LoadMoreButton";
import { Link, useRouter } from "@/i18n/navigation";
import { getErrorMessage } from "@/lib/apiClient";
import { getMySpots } from "@/lib/spotsApi";
import type { SpotSearchResult } from "@/lib/spotsApi";
import { UserRole } from "@/lib/userRole";

const PAGE_SIZE = 20;

const fetchMySpots = (page: number, signal: AbortSignal) => getMySpots(page, PAGE_SIZE, signal);

// Split out so the list (and its first request) only mounts once the page has confirmed the
// viewer is a signed-in non-admin.
function MySpotsList() {
  const t = useTranslations("MySpots");
  const list = usePagedList<SpotSearchResult>(fetchMySpots);
  const { items, status } = list;

  if (status === "loading") {
    return (
      <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {t("loading")}
      </p>
    );
  }

  if (list.error && items.length === 0) {
    return <p className="text-sm text-red-700 dark:text-red-400">{getErrorMessage(list.error, t("unknownError"))}</p>;
  }

  if (items.length === 0) {
    return (
      <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {t("empty")}{" "}
        <Link href="/map" className="underline">
          {t("emptyAddLink")}
        </Link>
      </p>
    );
  }

  return (
    <>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 md:grid-cols-3">
        {items.map((spot) => (
          <FeaturedSpotCard key={spot.id} spot={spot} moderationStatus={spot.moderationStatus} />
        ))}
      </div>
      <LoadMoreButton list={list} className="self-center" />
    </>
  );
}

export default function MySpotsPage() {
  const t = useTranslations("MySpots");
  const router = useRouter();
  const isAuthenticated = useRequireAuth();
  // Admins can't create spots, so there's nothing to list here for them.
  const isAdmin = useAuthStore((state) => state.user?.role === UserRole.Admin);

  useEffect(() => {
    if (isAuthenticated && isAdmin) {
      router.replace("/");
    }
  }, [isAuthenticated, isAdmin, router]);

  if (!isAuthenticated || isAdmin) {
    return null;
  }

  return (
    <div className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-4 p-8">
      <h1 className="text-2xl font-semibold">{t("pageTitle")}</h1>
      <MySpotsList />
    </div>
  );
}
