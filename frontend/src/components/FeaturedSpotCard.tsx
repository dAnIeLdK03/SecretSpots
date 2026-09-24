"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatRelativeTime } from "@/lib/relativeTime";
import type { SpotCategory } from "@/lib/spotsApi";
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
  }
}

export function FeaturedSpotCard({ spot }: FeaturedSpotCardProps) {
  const t = useTranslations("Spots");
  const locale = useLocale();
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
        {spot.distanceKm !== undefined ? (
          <p className="mt-2 text-xs" style={{ color: "var(--fieldmap-dim)" }}>
            {spot.distanceKm.toFixed(1)} km
          </p>
        ) : null}
      </div>
    </Link>
  );
}
