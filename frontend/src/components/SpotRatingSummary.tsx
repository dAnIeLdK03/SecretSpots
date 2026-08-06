"use client";

import { Star } from "lucide-react";
import { useTranslations } from "next-intl";

const STAR_VALUES = [1, 2, 3, 4, 5];

interface SpotRatingSummaryProps {
  averageRating: number;
  ratingsCount: number;
}

export function SpotRatingSummary({ averageRating, ratingsCount }: SpotRatingSummaryProps) {
  const t = useTranslations("Ratings");
  const roundedAverage = Math.round(averageRating);

  return (
    <div className="flex items-center gap-2">
      <div className="flex" aria-hidden="true">
        {STAR_VALUES.map((value) => (
          <Star
            key={value}
            size={18}
            style={{ color: "var(--fieldmap-gold)" }}
            fill={value <= roundedAverage ? "var(--fieldmap-gold)" : "none"}
          />
        ))}
      </div>
      <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {ratingsCount > 0
          ? t("summary", { average: averageRating.toFixed(1), count: ratingsCount })
          : t("noRatingsYet")}
      </span>
    </div>
  );
}
