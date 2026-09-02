"use client";

import { useEffect, useState } from "react";
import { Star } from "lucide-react";
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
  const [selected, setSelected] = useState<number | null>(null);
  const [hoverValue, setHoverValue] = useState<number | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authStatus !== "authenticated") {
      return;
    }

    const controller = new AbortController();
    fetchMyRating(spotId, controller.signal)
      .then((result) => {
        setMyRating(result.value);
        setSelected(result.value);
      })
      .catch(() => {});

    return () => controller.abort();
  }, [spotId, authStatus]);

  async function handleSubmit() {
    if (selected === null || submitting) return;

    setSubmitting(true);
    setError(null);
    try {
      const result = await rateSpot(spotId, selected);
      setMyRating(result.value);
      onRated({ averageRating: result.averageRating, ratingsCount: result.ratingsCount });
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setSubmitting(false);
    }
  }

  const displayValue = hoverValue ?? selected ?? 0;

  return (
    <div className="flex flex-col gap-2 rounded-2xl p-5 shadow-sm" style={{ backgroundColor: "var(--fieldmap-card)" }}>
      <span
        className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide"
        style={{ color: "var(--fieldmap-dim)" }}
      >
        <Star size={14} style={{ color: "var(--fieldmap-gold)" }} fill="var(--fieldmap-gold)" />
        {t("rateThisSpotLabel")}
      </span>

      {authStatus === "authenticated" ? (
        <>
          <div className="flex gap-1" onMouseLeave={() => setHoverValue(null)}>
            {STAR_VALUES.map((value) => (
              <button
                key={value}
                type="button"
                disabled={submitting}
                onMouseEnter={() => setHoverValue(value)}
                onClick={() => setSelected(value)}
                aria-label={t("rateWithStars", { count: value })}
                className="disabled:opacity-50"
              >
                <Star
                  size={28}
                  style={{ color: "var(--fieldmap-gold)" }}
                  fill={value <= displayValue ? "var(--fieldmap-gold)" : "none"}
                />
              </button>
            ))}
          </div>

          {selected ? (
            <div className="flex items-center gap-2 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
              <span>{t("yourRating", { value: selected })}</span>
              {selected !== myRating ? (
                <button
                  type="button"
                  onClick={handleSubmit}
                  disabled={submitting}
                  className="underline disabled:opacity-50"
                  style={{ color: "var(--fieldmap-trail)" }}
                >
                  {submitting ? t("submitting") : t("submitButton")}
                </button>
              ) : null}
            </div>
          ) : null}
          {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}
        </>
      ) : (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loginRequiredToRate")}{" "}
          <Link href="/login" className="underline">
            {tAuth("loginTitle")}
          </Link>
        </p>
      )}
    </div>
  );
}
