"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { createCheckIn } from "@/lib/checkInsApi";
import type { CheckInResponse } from "@/lib/checkInsApi";
import { ApiError, getErrorMessage } from "@/lib/apiClient";
import { PhotoUpload } from "@/components/PhotoUpload";

interface CheckInModalProps {
  spotId: string;
  onClose: () => void;
}

export function CheckInModal({ spotId, onClose }: CheckInModalProps) {
  const t = useTranslations("CheckIns");
  const [photoUrl, setPhotoUrl] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<CheckInResponse | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (typeof navigator === "undefined" || !navigator.geolocation) {
      setError(t("geolocationUnavailable"));
      return;
    }

    setSubmitting(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        void submitCheckIn(position.coords.latitude, position.coords.longitude);
      },
      () => {
        setError(t("geolocationDenied"));
        setSubmitting(false);
      },
    );
  }

  async function submitCheckIn(latitude: number, longitude: number) {
    try {
      const response = await createCheckIn(spotId, { photoUrl, latitude, longitude });
      setResult(response);
    } catch (err) {
      if (err instanceof ApiError && err.problem.code === "CheckIns.TooFarFromSpot") {
        setError(t("tooFarFromSpot"));
      } else {
        setError(getErrorMessage(err, t("unknownError")));
      }
      setSubmitting(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-sm rounded-lg bg-white p-6 dark:bg-zinc-900">
        {result ? (
          <div className="flex flex-col gap-4">
            <h2 className="text-lg font-semibold">{t("successTitle")}</h2>
            <p>{t("crystalsAwardedMessage", { count: result.crystalsAwarded })}</p>
            <p className="text-sm text-zinc-600 dark:text-zinc-400">
              {t("newBalanceLabel")}: {result.newCrystalBalance}
            </p>
            <div className="flex justify-end">
              <button
                type="button"
                onClick={onClose}
                className="rounded bg-zinc-900 px-4 py-2 text-white dark:bg-zinc-100 dark:text-zinc-900"
              >
                {t("closeButton")}
              </button>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <h2 className="text-lg font-semibold">{t("modalTitle")}</h2>
            <PhotoUpload label={t("photoLabel")} value={photoUrl} onChange={setPhotoUrl} />
            {error ? <p className="text-sm text-red-600 dark:text-red-400">{error}</p> : null}
            <div className="flex justify-end gap-2">
              <button type="button" onClick={onClose} className="rounded px-4 py-2 text-zinc-600 dark:text-zinc-400">
                {t("cancelButton")}
              </button>
              <button
                type="submit"
                disabled={submitting || !photoUrl}
                className="rounded bg-zinc-900 px-4 py-2 text-white disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900"
              >
                {submitting ? t("submitting") : t("submitButton")}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
