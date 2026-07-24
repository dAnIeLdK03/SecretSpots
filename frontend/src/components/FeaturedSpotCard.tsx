"use client";

import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatRelativeTime } from "@/lib/relativeTime";
import type { SpotCategory } from "@/lib/spotsApi";

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

  return (
    <Link
      href={`/spots/${spot.id}`}
      className="group flex flex-col overflow-hidden rounded-xl border border-zinc-200 bg-white shadow-sm transition hover:shadow-md dark:border-zinc-800 dark:bg-zinc-900"
    >
      <div className="relative h-40 w-full">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={spot.photoUrl} alt={spot.name} className="h-full w-full object-cover" />
        <span className="absolute top-2 left-2 rounded-full bg-white/90 px-2 py-1 text-xs font-medium text-zinc-900">
          {t(`category.${spot.category}`)}
        </span>
      </div>
      <div className="flex flex-1 flex-col gap-1 p-4">
        <h3 className="font-semibold text-zinc-900 dark:text-zinc-100">{spot.name}</h3>
        <p className="text-xs text-zinc-500">{formatRelativeTime(spot.createdAt, locale)}</p>
        <p className="mt-1 line-clamp-2 flex-1 text-sm text-zinc-600 dark:text-zinc-400">{spot.description}</p>
        {spot.distanceKm !== undefined ? (
          <p className="mt-2 text-xs text-zinc-500">{spot.distanceKm.toFixed(1)} km</p>
        ) : null}
      </div>
    </Link>
  );
}
