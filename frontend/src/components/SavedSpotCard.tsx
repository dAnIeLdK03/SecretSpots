"use client";

import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import type { SavedSpotResponse } from "@/lib/savedSpotsApi";

export function SavedSpotCard({ spot }: { spot: SavedSpotResponse }) {
  const t = useTranslations("Spots");

  return (
    <Link
      href={`/spots/${spot.spotId}`}
      className="group flex flex-col overflow-hidden rounded-xl border shadow-sm transition hover:shadow-md"
      style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-card)" }}
    >
      <div className="relative h-32 w-full">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={spot.photoUrl} alt={spot.spotName} className="h-full w-full object-cover" />
      </div>
      <div className="flex flex-1 flex-col gap-1 p-4" style={{ color: "var(--fieldmap-ink)" }}>
        <h3 className="font-semibold">{spot.spotName}</h3>
        <p className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
          {t(`category.${spot.category}`)}
        </p>
      </div>
    </Link>
  );
}
