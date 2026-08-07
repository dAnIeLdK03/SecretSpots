"use client";

import { useEffect, useState } from "react";
import { Bookmark } from "lucide-react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/store/useAuthStore";
import { saveSpot, unsaveSpot, fetchIsSpotSaved } from "@/lib/savedSpotsApi";

export function SaveSpotButton({ spotId }: { spotId: string }) {
  const t = useTranslations("SavedSpots");
  const authStatus = useAuthStore((state) => state.status);
  const [saved, setSaved] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (authStatus !== "authenticated") {
      return;
    }

    const controller = new AbortController();
    fetchIsSpotSaved(spotId, controller.signal)
      .then(setSaved)
      .catch(() => {});

    return () => controller.abort();
  }, [spotId, authStatus]);

  if (authStatus !== "authenticated") {
    return null;
  }

  async function handleToggle() {
    if (submitting) return;

    setSubmitting(true);
    const nextSaved = !saved;
    try {
      await (nextSaved ? saveSpot(spotId) : unsaveSpot(spotId));
      setSaved(nextSaved);
    } catch {
      // Leave the toggle as-is on failure — the user can just try again.
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <button
      type="button"
      onClick={handleToggle}
      disabled={submitting}
      aria-pressed={saved}
      className="inline-flex items-center gap-2 rounded px-4 py-2 text-sm disabled:opacity-50"
      style={
        saved
          ? { backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }
          : { backgroundColor: "var(--fieldmap-contour)", color: "var(--fieldmap-ink)" }
      }
    >
      <Bookmark size={16} fill={saved ? "currentColor" : "none"} />
      {saved ? t("savedButton") : t("saveButton")}
    </button>
  );
}
