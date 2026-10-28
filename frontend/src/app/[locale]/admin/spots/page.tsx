"use client";

import { useCallback, useEffect, useState } from "react";
import { useFormatter, useLocale, useTranslations } from "next-intl";
import { Check, ExternalLink, MapPin, RotateCcw, Search, ShieldCheck, Trash2, User, X, ZoomIn } from "lucide-react";
import { Link } from "@/i18n/navigation";
import { getErrorMessage } from "@/lib/apiClient";
import { approveSpot, getAdminSpotCounts, getAdminSpots, rejectSpot } from "@/lib/adminApi";
import type { AdminSpot, AdminSpotCounts } from "@/lib/adminApi";
import { SPOT_MODERATION_STATUSES } from "@/lib/spotsApi";
import type { SpotModerationStatus } from "@/lib/spotsApi";
import { formatRelativeTime } from "@/lib/relativeTime";
import { CategoryIcon } from "@/components/CategoryIcon";
import { ImageLightbox } from "@/components/ImageLightbox";
import { ModerationStatusBadge } from "@/components/ModerationStatusBadge";
import { RejectSpotModal } from "@/components/RejectSpotModal";
import { LoadMoreButton } from "@/components/LoadMoreButton";
import { usePagedList } from "@/hooks/usePagedList";

const PAGE_SIZE = 20;
const SEARCH_DEBOUNCE_MS = 300;

const STATUS_COUNT_KEYS: Record<SpotModerationStatus, keyof AdminSpotCounts> = {
  Pending: "pending",
  Approved: "approved",
  Rejected: "rejected",
};


export default function AdminSpotsPage() {
  const t = useTranslations("AdminSpots");
  const tSpots = useTranslations("Spots");
  const locale = useLocale();
  const format = useFormatter();

  const [statusFilter, setStatusFilter] = useState<SpotModerationStatus>("Pending");
  const [search, setSearch] = useState("");
  // What the list is actually filtered by — lags `search` by SEARCH_DEBOUNCE_MS so typing
  // doesn't fire a request per keystroke.
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [counts, setCounts] = useState<AdminSpotCounts | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busyIds, setBusyIds] = useState<Set<string>>(new Set());
  const [rejectingSpot, setRejectingSpot] = useState<AdminSpot | null>(null);
  const [lightboxSpot, setLightboxSpot] = useState<AdminSpot | null>(null);

  useEffect(() => {
    const timeoutId = setTimeout(() => setDebouncedSearch(search.trim()), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timeoutId);
  }, [search]);

  const refreshCounts = useCallback(() => {
    getAdminSpotCounts()
      .then(setCounts)
      .catch(() => {
        // Counts are decoration on the filter — the list itself still works without them.
      });
  }, []);

  const fetchSpots = useCallback(
    (page: number, signal: AbortSignal) =>
      getAdminSpots(page, PAGE_SIZE, { status: statusFilter, q: debouncedSearch || undefined }, signal),
    [statusFilter, debouncedSearch],
  );
  const list = usePagedList(fetchSpots);
  const { items, status } = list;
  const error = actionError ?? (list.error ? getErrorMessage(list.error, t("unknownError")) : null);

  useEffect(() => {
    refreshCounts();
  }, [refreshCounts]);

  // Every action moves a spot out of the currently filtered status, so it leaves this list.
  function removeFromList(id: string) {
    list.removeItem(id);
    refreshCounts();
  }

  function setBusy(id: string, busy: boolean) {
    setBusyIds((prev) => {
      const next = new Set(prev);
      if (busy) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  async function handleApprove(id: string) {
    setBusy(id, true);
    try {
      await approveSpot(id);
      removeFromList(id);
    } catch (err) {
      setActionError(getErrorMessage(err, t("unknownError")));
    } finally {
      setBusy(id, false);
    }
  }

  async function handleReject(reason: string) {
    if (!rejectingSpot) return;
    const id = rejectingSpot.id;
    setBusy(id, true);
    try {
      await rejectSpot(id, reason);
      removeFromList(id);
      setRejectingSpot(null);
    } catch (err) {
      setActionError(getErrorMessage(err, t("unknownError")));
    } finally {
      setBusy(id, false);
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-5xl flex-1 flex-col gap-6 p-4 md:p-8">
      <div className="flex flex-col justify-between gap-4 border-b pb-4 md:flex-row md:items-center border-contour">
        <div>
          <h1 className="flex items-center gap-2 text-2xl font-semibold">
            <ShieldCheck size={24} className="text-trail" />
            {t("title")}
          </h1>
          <p className="text-sm text-dim">{t("subtitle")}</p>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as SpotModerationStatus)}
            aria-label={t("statusFilterLabel")}
            className="rounded-lg border px-3 py-2 text-sm border-contour bg-card"
          >
            {SPOT_MODERATION_STATUSES.map((s) => (
              <option key={s} value={s}>
                {counts
                  ? t("statusWithCount", { status: t(`status.${s}`), count: counts[STATUS_COUNT_KEYS[s]] })
                  : t(`status.${s}`)}
              </option>
            ))}
          </select>

        </div>
      </div>

      <label className="relative block">
        <Search
          size={16}
          className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-dim"
        />
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t("searchPlaceholder", { status: t(`status.${statusFilter}`).toLowerCase() })}
          aria-label={t("searchLabel")}
          className="w-full rounded-lg border py-2 pr-3 pl-9 text-sm border-contour bg-card"
        />
      </label>

      {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}

      {status === "loading" ? (
        <p className="text-sm text-dim">{t("loading")}</p>
      ) : items.length === 0 ? (
        <p className="rounded-xl p-8 text-center text-sm bg-card text-dim">
          {debouncedSearch ? t("noSearchResults", { query: debouncedSearch }) : t("empty")}
        </p>
      ) : (
        <ul className="flex flex-col gap-4">
          {items.map((spot) => {
            const busy = busyIds.has(spot.id);
            return (
              <li
                key={spot.id}
                className={`rounded-xl border bg-card p-4 transition ${
                  spot.moderationStatus === "Rejected" ? "border-red-500/35" : "border-contour"
                } ${busy ? "opacity-60" : ""}`}
              >
                <div className="flex flex-col items-start gap-4 md:flex-row">
                  <button
                    type="button"
                    onClick={() => setLightboxSpot(spot)}
                    aria-label={t("enlargePhoto")}
                    className="group relative h-32 w-full shrink-0 overflow-hidden rounded-lg border md:w-48 border-contour bg-contour"
                  >
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={spot.photoUrl}
                      alt={spot.name}
                      className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                    />
                    <span className="pointer-events-none absolute inset-0 flex items-center justify-center gap-1 bg-black/30 text-xs text-white opacity-0 transition-opacity group-hover:opacity-100">
                      <ZoomIn size={14} />
                      {t("enlargePhoto")}
                    </span>
                  </button>

                  <div className="min-w-0 flex-1 space-y-2">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <div className="flex flex-wrap items-center gap-2 text-xs">
                        <ModerationStatusBadge status={spot.moderationStatus} />
                        <span className="flex items-center gap-1 text-dim">
                          <CategoryIcon category={spot.category} size={12} />
                          {tSpots(`category.${spot.category}`)}
                        </span>
                        <span className="text-dim">• {formatRelativeTime(spot.createdAt, locale)}</span>
                      </div>
                      <Link
                        href={`/admin/users/${spot.createdByUserId}`}
                        className="flex items-center gap-1 text-xs hover:underline text-dim"
                      >
                        <User size={12} />
                        {spot.createdByDisplayName}
                      </Link>
                    </div>

                    <div className="space-y-1">
                      <h2 className="text-lg font-semibold break-words">{spot.name}</h2>
                      <p className="line-clamp-2 text-sm break-words text-dim">{spot.description}</p>
                    </div>

                    {spot.rejectionReason || spot.scheduledDeletionAt ? (
                      <div className="space-y-0.5 rounded-lg border border-red-300 bg-red-50 px-3 py-2 text-xs text-red-700 dark:border-red-800 dark:bg-red-950 dark:text-red-400">
                        {spot.rejectionReason ? (
                          <p>
                            <span className="font-semibold">{t("rejectionReasonLabel")}:</span> {spot.rejectionReason}
                          </p>
                        ) : null}
                        {spot.scheduledDeletionAt ? (
                          <p>
                            {t("scheduledDeletionLabel", {
                              date: format.dateTime(new Date(spot.scheduledDeletionAt), { dateStyle: "medium", timeStyle: "short" }),
                            })}
                          </p>
                        ) : null}
                      </div>
                    ) : null}

                    <div className="flex flex-wrap items-center gap-4 pt-1 text-xs text-dim">
                      <Link
                        href={`/map?lat=${spot.latitude}&lng=${spot.longitude}&spot=${spot.id}`}
                        className="flex items-center gap-1 hover:underline"
                      >
                        <MapPin size={12} />
                        {t("viewOnMapLink")}
                      </Link>
                      <Link
                        href={`/spots/${spot.id}`}
                        className="flex items-center gap-1 hover:underline text-trail"
                      >
                        <ExternalLink size={12} />
                        {t("viewSpotLink")}
                      </Link>
                    </div>
                  </div>

                  <div
                    className="flex w-full justify-end gap-2 border-t pt-3 md:w-auto md:flex-col md:border-t-0 md:pt-0 border-contour"
                  >
                    {spot.moderationStatus !== "Approved" ? (
                      <button
                        onClick={() => handleApprove(spot.id)}
                        disabled={busy}
                        className="flex flex-1 items-center justify-center gap-2 rounded-lg px-4 py-2.5 text-sm font-medium shadow-sm transition hover:opacity-90 disabled:opacity-50 md:flex-none bg-ink text-paper-light"
                      >
                        {/* Approving a Rejected spot undoes the rejection, so it reads as a restore. */}
                        {spot.moderationStatus === "Rejected" ? (
                          <>
                            <RotateCcw size={16} />
                            {t("restoreButton")}
                          </>
                        ) : (
                          <>
                            <Check size={16} />
                            {t("approveButton")}
                          </>
                        )}
                      </button>
                    ) : null}
                    {spot.moderationStatus !== "Rejected" ? (
                      <button
                        onClick={() => setRejectingSpot(spot)}
                        disabled={busy}
                        className="flex flex-1 items-center justify-center gap-2 rounded-lg border border-red-300 bg-red-50 px-4 py-2.5 text-sm font-medium text-red-700 transition hover:bg-red-100 disabled:opacity-50 md:flex-none dark:border-red-800 dark:bg-red-950/40 dark:text-red-400 dark:hover:bg-red-950/70"
                      >
                        {/* Rejecting an Approved spot takes it down, so it reads as a removal. */}
                        {spot.moderationStatus === "Approved" ? (
                          <>
                            <Trash2 size={16} />
                            {t("removeButton")}
                          </>
                        ) : (
                          <>
                            <X size={16} />
                            {t("rejectButton")}
                          </>
                        )}
                      </button>
                    ) : null}
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <LoadMoreButton list={list} className="self-center" />

      {rejectingSpot ? (
        <RejectSpotModal
          spotName={rejectingSpot.name}
          isRemoval={rejectingSpot.moderationStatus === "Approved"}
          busy={busyIds.has(rejectingSpot.id)}
          onConfirm={handleReject}
          onClose={() => setRejectingSpot(null)}
        />
      ) : null}

      {lightboxSpot ? (
        <ImageLightbox src={lightboxSpot.photoUrl} alt={lightboxSpot.name} onClose={() => setLightboxSpot(null)} />
      ) : null}
    </div>
  );
}
