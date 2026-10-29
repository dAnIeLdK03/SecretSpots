"use client";

import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import type { SavedSpotResponse } from "@/lib/savedSpotsApi";
import { CategoryIcon } from "./CategoryIcon";

export function SavedSpotCard({ spot }: { spot: SavedSpotResponse }) {
  const t = useTranslations("Spots");

  return (
    <Link
      href={`/spots/${spot.spotId}`}
      className="group flex flex-col overflow-hidden rounded-xl border shadow-sm transition hover:shadow-md border-contour bg-card"
    >
      <div className="relative h-32 w-full">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={spot.photoUrl}
          alt={spot.spotName}
          loading="lazy"
          decoding="async"
          className="h-full w-full object-cover"
        />
      </div>
      <div className="flex flex-1 flex-col gap-1 p-4 text-ink">
        <h3 className="font-semibold">{spot.spotName}</h3>
        <div className="flex items-center gap-1 text-xs text-dim">
          <CategoryIcon category={spot.category} size={12} />
          {t(`category.${spot.category}`)}
        </div>
      </div>
    </Link>
  );
}
