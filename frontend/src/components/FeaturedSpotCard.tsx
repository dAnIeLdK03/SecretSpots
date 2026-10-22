"use client";

import { useState } from "react";
import { useFormatter, useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatRelativeTime } from "@/lib/relativeTime";
import type { SpotCategory, SpotModerationStatus } from "@/lib/spotsApi";
import { CategoryIcon } from "./CategoryIcon";

interface FeaturedSpotCardProps {
  spot: {
    id: string;
    name: string;
    description: string;
    category: SpotCategory;
    photoUrl: string;
    createdAt: string;
    distanceKm?: number;
    rejectionReason?: string | null;
    scheduledDeletionAt?: string | null;
  };
  // Only passed by the creator's own list (My spots) — public lists never show moderation state.
  moderationStatus?: SpotModerationStatus;
}

export function FeaturedSpotCard({ spot, moderationStatus }: FeaturedSpotCardProps) {
  const t = useTranslations("Spots");
  const locale = useLocale();
  const format = useFormatter();
  const [imageFailed, setImageFailed] = useState(false);

  return (
    <Link
      href={`/spots/${spot.id}`}
      className="group flex min-w-0 flex-col overflow-hidden rounded-xl border shadow-sm transition hover:shadow-md"
      style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-card)" }}
    >
      <div className="relative h-40 w-full shrink-0 overflow-hidden" style={{ backgroundColor: "var(--fieldmap-contour)" }}>
        {!imageFailed ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={spot.photoUrl}
            alt={spot.name}
            onError={() => setImageFailed(true)}
            className="h-full w-full object-cover"
          />
        ) : null}
        <span
          className="absolute top-2 left-2 flex items-center gap-1.5 rounded-full px-2 py-1 text-xs font-semibold"
          style={{ backgroundColor: "rgba(241, 237, 220, 0.9)", color: "var(--fieldmap-ink)" }}
        >
          <CategoryIcon category={spot.category} size={12} />
          {t(`category.${spot.category}`)}
        </span>
      </div>
      <div className="flex flex-1 flex-col gap-1 p-4" style={{ color: "var(--fieldmap-ink)" }}>
        <h3 className="font-semibold break-words">{spot.name}</h3>
        <p className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
          {formatRelativeTime(spot.createdAt, locale)}
        </p>
        <p className="mt-1 line-clamp-2 flex-1 text-sm break-words" style={{ color: "var(--fieldmap-dim)" }}>
          {spot.description}
        </p>
        {moderationStatus === "Rejected" ? (
          <div className="mt-2 flex flex-col gap-1 text-xs text-red-700 dark:text-red-400">
            <span className="self-start rounded-full border border-red-300 dark:border-red-800 px-2 py-0.5 font-semibold">
              {t("moderationStatusRejected")}
            </span>
            {spot.rejectionReason ? (
              <span className="line-clamp-2 break-words">
                {t("rejectionReasonLabel")}: {spot.rejectionReason}
              </span>
            ) : null}
            {spot.scheduledDeletionAt ? (
              <span>
                {t("scheduledDeletionNotice", {
                  date: format.dateTime(new Date(spot.scheduledDeletionAt), { dateStyle: "medium", timeStyle: "short" }),
                })}
              </span>
            ) : null}
          </div>
        ) : moderationStatus === "Pending" ? (
          <span
            className="mt-2 self-start rounded-full px-2 py-0.5 text-xs font-semibold"
            style={{ backgroundColor: "var(--fieldmap-contour)", color: "var(--fieldmap-ink)" }}
          >
            {t("moderationStatusPending")}
          </span>
        ) : null}
        {spot.distanceKm !== undefined ? (
          <p className="mt-2 text-xs" style={{ color: "var(--fieldmap-dim)" }}>
            {spot.distanceKm.toFixed(1)} km
          </p>
        ) : null}
      </div>
    </Link>
  );
}
