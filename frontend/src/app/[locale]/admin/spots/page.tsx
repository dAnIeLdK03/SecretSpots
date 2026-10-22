"use client";

import { useCallback, useEffect, useState } from "react";
import { useFormatter, useLocale, useTranslations } from "next-intl";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { useAuthStore } from "@/store/useAuthStore";
import { Link, useRouter } from "@/i18n/navigation";
import { getErrorMessage } from "@/lib/apiClient";
import { approveSpot, getAdminSpots, rejectSpot } from "@/lib/adminApi";
import { SPOT_MODERATION_STATUSES } from "@/lib/spotsApi";
import type { SpotModerationStatus, SpotSearchResult } from "@/lib/spotsApi";
import { formatRelativeTime } from "@/lib/relativeTime";
import { UserRole } from "@/lib/userRole";

const PAGE_SIZE = 20;
// Mirrors RejectSpot.MaxReasonLength on the backend.
const MAX_REJECTION_REASON_LENGTH = 500;

export default function AdminSpotsPage() {
  const t = useTranslations("AdminSpots");
  const tSpots = useTranslations("Spots");
  const locale = useLocale();
  const format = useFormatter();
  const router = useRouter();
  const isAuthenticated = useRequireAuth();
  const user = useAuthStore((state) => state.user);

  const [statusFilter, setStatusFilter] = useState<SpotModerationStatus>("Pending");
  const [items, setItems] = useState<SpotSearchResult[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<"loading" | "idle" | "loadingMore">("loading");
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [rejectionReason, setRejectionReason] = useState("");

  useEffect(() => {
    if (isAuthenticated && user && user.role !== UserRole.Admin) {
      router.replace("/");
    }
  }, [isAuthenticated, user, router]);

  const fetchPage = useCallback(
    (pageNum: number, append: boolean) => {
      setStatus(append ? "loadingMore" : "loading");
      return getAdminSpots(pageNum, PAGE_SIZE, { status: statusFilter })
        .then((result) => {
          setItems((prev) => (append ? [...prev, ...result.items] : result.items));
          setTotalCount(result.totalCount);
          setPage(pageNum);
          setError(null);
        })
        .catch((err) => setError(getErrorMessage(err, t("unknownError"))))
        .finally(() => setStatus("idle"));
    },
    [statusFilter, t],
  );

  useEffect(() => {
    if (isAuthenticated && user?.role === UserRole.Admin) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- kicking off the initial fetch on mount, no user event to attach to
      void fetchPage(1, false);
    }
  }, [isAuthenticated, user?.role, fetchPage]);

  // Every action moves the spot out of the currently filtered status, so it leaves this list.
  function removeFromList(id: string) {
    setItems((prev) => prev.filter((s) => s.id !== id));
    setTotalCount((prev) => prev - 1);
  }

  async function handleApprove(id: string) {
    setBusyId(id);
    try {
      await approveSpot(id);
      removeFromList(id);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setBusyId(null);
    }
  }

  function startRejecting(id: string) {
    setRejectingId(id);
    setRejectionReason("");
  }

  async function handleReject(id: string) {
    setBusyId(id);
    try {
      await rejectSpot(id, rejectionReason.trim());
      removeFromList(id);
      setRejectingId(null);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setBusyId(null);
    }
  }

  if (!isAuthenticated || user?.role !== UserRole.Admin) {
    return null;
  }

  const hasMore = items.length < totalCount;

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-1 flex-col gap-4 p-8">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-semibold">{t("title")}</h1>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as SpotModerationStatus)}
          aria-label={t("statusFilterLabel")}
          className="rounded border px-2 py-1 text-sm"
          style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-card)" }}
        >
          {SPOT_MODERATION_STATUSES.map((s) => (
            <option key={s} value={s}>
              {t(`status.${s}`)}
            </option>
          ))}
        </select>
      </div>

      {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}

      {status === "loading" ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loading")}
        </p>
      ) : items.length === 0 ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("empty")}
        </p>
      ) : (
        <ul className="flex flex-col gap-3">
          {items.map((spot) => (
            <li
              key={spot.id}
              className="flex flex-col gap-3 rounded-2xl p-4 sm:flex-row"
              style={{ backgroundColor: "var(--fieldmap-card)" }}
            >
              <div
                className="h-32 w-full shrink-0 overflow-hidden rounded-xl sm:w-40"
                style={{ backgroundColor: "var(--fieldmap-contour)" }}
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={spot.photoUrl} alt={spot.name} className="h-full w-full object-cover" />
              </div>

              <div className="flex min-w-0 flex-1 flex-col gap-2">
                <div className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
                  {tSpots(`category.${spot.category}`)} · {formatRelativeTime(spot.createdAt, locale)}
                </div>
                <h2 className="text-sm font-semibold">{spot.name}</h2>
                <p className="text-sm break-words" style={{ color: "var(--fieldmap-ink)" }}>
                  {spot.description}
                </p>

                {spot.rejectionReason ? (
                  <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
                    {t("rejectionReasonLabel")}: {spot.rejectionReason}
                  </p>
                ) : null}

                {spot.scheduledDeletionAt ? (
                  <p className="text-xs text-red-700 dark:text-red-400">
                    {t("scheduledDeletionLabel", {
                      date: format.dateTime(new Date(spot.scheduledDeletionAt), { dateStyle: "medium", timeStyle: "short" }),
                    })}
                  </p>
                ) : null}

                {rejectingId === spot.id ? (
                  <div className="flex flex-col gap-2">
                    <textarea
                      value={rejectionReason}
                      onChange={(e) => setRejectionReason(e.target.value)}
                      maxLength={MAX_REJECTION_REASON_LENGTH}
                      placeholder={t("rejectionReasonPlaceholder")}
                      rows={3}
                      className="rounded border px-3 py-2 text-sm"
                      style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
                    />
                    <div className="flex flex-wrap gap-3 text-sm">
                      <button
                        onClick={() => handleReject(spot.id)}
                        disabled={busyId === spot.id || rejectionReason.trim().length === 0}
                        className="rounded border border-red-300 dark:border-red-800 px-3 py-1 text-red-700 dark:text-red-400 disabled:opacity-50"
                      >
                        {t("confirmRejectButton")}
                      </button>
                      <button
                        onClick={() => setRejectingId(null)}
                        disabled={busyId === spot.id}
                        className="rounded border px-3 py-1 disabled:opacity-50"
                        style={{ borderColor: "var(--fieldmap-contour)" }}
                      >
                        {t("cancelButton")}
                      </button>
                    </div>
                  </div>
                ) : (
                  <div className="flex flex-wrap items-center gap-3 text-sm">
                    <Link href={`/spots/${spot.id}`} className="underline">
                      {t("viewSpotLink")}
                    </Link>
                    <Link href={`/admin/users/${spot.createdByUserId}`} className="underline">
                      {t("viewCreatorLink")}
                    </Link>
                    {spot.moderationStatus !== "Approved" ? (
                      <button
                        onClick={() => handleApprove(spot.id)}
                        disabled={busyId === spot.id}
                        className="rounded px-3 py-1 disabled:opacity-50"
                        style={{ backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper-light)" }}
                      >
                        {t("approveButton")}
                      </button>
                    ) : null}
                    {spot.moderationStatus !== "Rejected" ? (
                      <button
                        onClick={() => startRejecting(spot.id)}
                        disabled={busyId === spot.id}
                        className="rounded border border-red-300 dark:border-red-800 px-3 py-1 text-red-700 dark:text-red-400 disabled:opacity-50"
                      >
                        {t("rejectButton")}
                      </button>
                    ) : null}
                  </div>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      {hasMore ? (
        <button
          onClick={() => fetchPage(page + 1, true)}
          disabled={status === "loadingMore"}
          className="rounded border px-4 py-2 text-sm disabled:opacity-50"
          style={{ borderColor: "var(--fieldmap-contour)" }}
        >
          {status === "loadingMore" ? t("loadingMore") : t("loadMore")}
        </button>
      ) : null}
    </div>
  );
}
