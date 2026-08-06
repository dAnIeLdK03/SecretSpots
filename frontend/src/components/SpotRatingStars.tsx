"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/store/useAuthStore";
import { rateSpot, fetchMyRating } from "@/lib/ratingsApi";
import { getErrorMessage } from "@/lib/apiClient";

const STAR_VALUES = [1, 2, 3, 4, 5];

interface SpotRatingStarsProps {
  spotId: string;
  averageRating: number;
  ratingsCount: number;
  onRated: (stats: { averageRating: number; ratingsCount: number }) => void;
}

export function SpotRatingStars({ spotId, averageRating, ratingsCount, onRated }: SpotRatingStarsProps) {
  const t = useTranslations("Ratings");
  const authStatus = useAuthStore((state) => state.status);
  const [myRating, setMyRating] = useState<number | null>(null);
  const [hoverValue, setHoverValue] = useState<number | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authStatus !== "authenticated") {
      return;
    }

    const controller = new AbortController();
    fetchMyRating(spotId, controller.signal)
      .then((result) => setMyRating(result.value))
      .catch(() => {});

    return () => controller.abort();
  }, [spotId, authStatus]);

  async function handleRate(value: number) {
    if (submitting) return;

    setSubmitting(true);
    setError(null);
    try {
      const result = await rateSpot(spotId, value);
      setMyRating(result.value);
      onRated({ averageRating: result.averageRating, ratingsCount: result.ratingsCount });
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setSubmitting(false);
    }
  }

  const roundedAverage = Math.round(averageRating);
  const displayValue = hoverValue ?? myRating ?? 0;

  return (
    <div className="flex flex-col gap-1">
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

      {authStatus === "authenticated" ? (
        <div className="flex items-center gap-2">
          <div onMouseLeave={() => setHoverValue(null)}>
            {STAR_VALUES.map((value) => (
              <button
                key={value}
                type="button"
                disabled={submitting}
                onMouseEnter={() => setHoverValue(value)}
                onClick={() => handleRate(value)}
                aria-label={t("rateWithStars", { count: value })}
                className="disabled:opacity-50"
                style={{ color: value <= displayValue ? "var(--fieldmap-trail)" : "var(--fieldmap-contour)" }}
              >
                ★
              </button>
            ))}
          </div>
          {myRating ? (
            <span className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
              {t("yourRating", { value: myRating })}
            </span>
          ) : null}
        </div>
      ) : null}
      {error ? <p className="text-sm text-red-700">{error}</p> : null}
    </div>
  );
}
