"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/store/useAuthStore";
import { rateSpot, fetchMyRating } from "@/lib/ratingsApi";
import { getErrorMessage } from "@/lib/apiClient";
import { Link } from "@/i18n/navigation";

const STAR_VALUES = [1, 2, 3, 4, 5];

interface SpotRatingInputProps {
  spotId: string;
  onRated: (stats: { averageRating: number; ratingsCount: number }) => void;
}

export function SpotRatingInput({ spotId, onRated }: SpotRatingInputProps) {
  const t = useTranslations("Ratings");
  const tAuth = useTranslations("Auth");
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

  if (authStatus !== "authenticated") {
    return (
      <div
        className="mt-3 flex w-fit flex-wrap items-center gap-1 rounded border px-3 py-2 text-sm"
        style={{ borderColor: "var(--fieldmap-contour)", color: "var(--fieldmap-dim)" }}
      >
        <span>{t("loginRequiredToRate")}</span>
        <Link href="/login" className="underline">
          {tAuth("loginTitle")}
        </Link>
      </div>
    );
  }

  const displayValue = hoverValue ?? myRating ?? 0;

  return (
    <div
      className="mt-3 flex w-fit flex-col gap-1 rounded border px-3 py-2"
      style={{ borderColor: "var(--fieldmap-contour)" }}
    >
      <div className="flex items-center gap-2">
        <span className="text-xs font-medium uppercase tracking-wide" style={{ color: "var(--fieldmap-dim)" }}>
          {t("rateThisSpotLabel")}
        </span>
        {myRating ? (
          <span className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
            {t("yourRating", { value: myRating })}
          </span>
        ) : null}
      </div>
      <div onMouseLeave={() => setHoverValue(null)}>
        {STAR_VALUES.map((value) => (
          <button
            key={value}
            type="button"
            disabled={submitting}
            onMouseEnter={() => setHoverValue(value)}
            onClick={() => handleRate(value)}
            aria-label={t("rateWithStars", { count: value })}
            className="text-2xl leading-none disabled:opacity-50"
            style={{ color: value <= displayValue ? "var(--fieldmap-trail)" : "var(--fieldmap-contour)" }}
          >
            ★
          </button>
        ))}
      </div>
      {error ? <p className="text-sm text-red-700">{error}</p> : null}
    </div>
  );
}
