"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ImagePlus } from "lucide-react";
import { addCheckInPhotoToSpot, getAdminSpotCheckIns } from "@/lib/adminApi";
import { getErrorMessage } from "@/lib/apiClient";
import { formatRelativeTime } from "@/lib/relativeTime";
import { MAX_SPOT_PHOTOS } from "@/lib/spotsApi";
import { usePagedList } from "@/hooks/usePagedList";
import { useToast } from "@/hooks/useToast";
import { ErrorMessage } from "@/components/ErrorMessage";
import { LoadMoreButton } from "@/components/LoadMoreButton";
import { StatusMessage } from "@/components/StatusMessage";

const PAGE_SIZE = 12;

interface AdminSpotCheckInPhotosProps {
  spotId: string;
  // How many photos the spot has now — adding stops at MAX_SPOT_PHOTOS.
  photoCount: number;
  onPhotoAdded: (photoUrl: string) => void;
}

// Admin-only section on the spot page: visitors' check-in photos, each one addable to the spot's
// own photos (the visitor gets a notification). Removing one again goes through a normal edit.
export function AdminSpotCheckInPhotos({ spotId, photoCount, onPhotoAdded }: AdminSpotCheckInPhotosProps) {
  const t = useTranslations("AdminSpotCheckIns");
  const locale = useLocale();
  const showToast = useToast();
  const [addedIds, setAddedIds] = useState<Set<string>>(new Set());
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const fetchCheckIns = useCallback(
    (page: number, signal: AbortSignal) => getAdminSpotCheckIns(spotId, page, PAGE_SIZE, signal),
    [spotId],
  );
  const list = usePagedList(fetchCheckIns);
  const error = actionError ?? (list.error ? getErrorMessage(list.error, t("unknownError")) : null);
  const spotFull = photoCount >= MAX_SPOT_PHOTOS;

  async function handleAdd(checkInId: string, photoUrl: string) {
    setBusyId(checkInId);
    setActionError(null);
    try {
      await addCheckInPhotoToSpot(checkInId);
      setAddedIds((prev) => new Set(prev).add(checkInId));
      onPhotoAdded(photoUrl);
      showToast(t("toastAdded"));
    } catch (err) {
      setActionError(getErrorMessage(err, t("unknownError")));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <section className="flex flex-col gap-3 rounded-2xl bg-card p-5">
      <div>
        <h2 className="font-semibold">
          {t("title")}
          {list.status !== "loading" ? <span className="ml-1 font-normal text-dim">({list.totalCount})</span> : null}
        </h2>
        <p className="text-sm text-dim">{t("hint")}</p>
      </div>

      {error ? <ErrorMessage>{error}</ErrorMessage> : null}
      {spotFull ? <p className="text-sm text-dim">{t("spotFull", { max: MAX_SPOT_PHOTOS })}</p> : null}

      {list.status === "loading" ? (
        <StatusMessage>{t("loading")}</StatusMessage>
      ) : list.items.length === 0 ? (
        <p className="text-sm text-dim">{t("empty")}</p>
      ) : (
        <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          {list.items.map((checkIn) => {
            const onSpot = checkIn.isOnSpot || addedIds.has(checkIn.id);
            return (
              <li key={checkIn.id} className="flex flex-col gap-1.5">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={checkIn.photoUrl}
                  alt={t("photoAlt", { name: checkIn.userDisplayName })}
                  loading="lazy"
                  decoding="async"
                  className="aspect-square w-full rounded-lg object-cover"
                />
                <span className="truncate text-xs text-dim">
                  {checkIn.userDisplayName} · {formatRelativeTime(checkIn.createdAt, locale)}
                </span>
                {onSpot ? (
                  <span className="self-start rounded-full bg-gold px-2 py-0.5 text-xs font-semibold text-[#2b2a23]">
                    {t("onSpotBadge")}
                  </span>
                ) : (
                  <button
                    type="button"
                    onClick={() => void handleAdd(checkIn.id, checkIn.photoUrl)}
                    disabled={spotFull || busyId !== null}
                    className="flex items-center justify-center gap-1.5 rounded border border-contour px-2 py-1.5 text-xs disabled:opacity-50"
                  >
                    <ImagePlus size={14} aria-hidden="true" />
                    {t("addButton")}
                  </button>
                )}
              </li>
            );
          })}
        </ul>
      )}

      <LoadMoreButton list={list} className="self-center" />
    </section>
  );
}
