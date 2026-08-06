"use client";

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
      <div aria-hidden="true">
        {STAR_VALUES.map((value) => (
          <span
            key={value}
            style={{ color: value <= roundedAverage ? "var(--fieldmap-trail)" : "var(--fieldmap-contour)" }}
          >
            ★
          </span>
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
