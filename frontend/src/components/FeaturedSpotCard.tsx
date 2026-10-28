"use client";

import { useState } from "react";
import { useFormatter, useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatRelativeTime } from "@/lib/relativeTime";
import type { SpotCategory, SpotModerationStatus } from "@/lib/spotsApi";
import { CategoryIcon } from "./CategoryIcon";
import { ModerationStatusBadge } from "./ModerationStatusBadge";

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
      className="group flex min-w-0 flex-col overflow-hidden rounded-xl border shadow-sm transition hover:shadow-md border-contour bg-card"
    >
      <div className="relative h-40 w-full shrink-0 overflow-hidden bg-contour">
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
          className="absolute top-2 left-2 flex items-center gap-1.5 rounded-full bg-[rgba(241,237,220,0.9)] px-2 py-1 text-xs font-semibold text-ink"
        >
          <CategoryIcon category={spot.category} size={12} />
          {t(`category.${spot.category}`)}
        </span>
      </div>
      <div className="flex flex-1 flex-col gap-1 p-4 text-ink">
        <h3 className="font-semibold break-words">{spot.name}</h3>
        <p className="text-xs text-dim">{formatRelativeTime(spot.createdAt, locale)}</p>
        <p className="mt-1 line-clamp-2 flex-1 text-sm break-words text-dim">{spot.description}</p>
        {moderationStatus === "Rejected" ? (
          <div className="mt-2 flex flex-col gap-1 text-xs text-red-700 dark:text-red-400">
            <ModerationStatusBadge status="Rejected" className="self-start" />
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
          <ModerationStatusBadge status="Pending" className="mt-2 self-start" />
        ) : null}
        {spot.distanceKm !== undefined ? (
          <p className="mt-2 text-xs text-dim">{spot.distanceKm.toFixed(1)} km</p>
        ) : null}
      </div>
    </Link>
  );
}
