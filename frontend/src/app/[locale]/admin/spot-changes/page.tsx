"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ArrowRight, Check, ExternalLink, FilePenLine, Trash2, User, X } from "lucide-react";
import { Link } from "@/i18n/navigation";
import { getErrorMessage } from "@/lib/apiClient";
import {
  approveSpotChangeRequest,
  getAdminSpotChangeRequests,
  MAX_REJECTION_REASON_LENGTH,
  rejectSpotChangeRequest,
} from "@/lib/adminApi";
import type { AdminSpotChangeRequest } from "@/lib/adminApi";
import { formatRelativeTime } from "@/lib/relativeTime";
import { ImageLightbox } from "@/components/ImageLightbox";
import { LoadMoreButton } from "@/components/LoadMoreButton";
import { usePagedList } from "@/hooks/usePagedList";
import { ErrorMessage } from "@/components/ErrorMessage";
import { StatusMessage } from "@/components/StatusMessage";

const PAGE_SIZE = 20;

// No filters on this queue, so the fetcher is stable and the list loads once on mount.
const fetchChangeRequests = (page: number, signal: AbortSignal) => getAdminSpotChangeRequests(page, PAGE_SIZE, signal);

function PhotoStrip({
  photoUrls,
  highlight,
  highlightLabel,
  highlightTone,
  label,
  photoAlt,
  onOpen,
}: {
  photoUrls: string[];
  // Photos that differ from the other side: removed ones on the current strip, added on the proposed.
  highlight: Set<string>;
  highlightLabel?: string;
  highlightTone?: "removed" | "added";
  label: string;
  // Accessible name for the photo at a 1-based position, e.g. "Photo 2 of Krushuna".
  photoAlt: (position: number) => string;
  onOpen: (photo: { url: string; alt: string }) => void;
}) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-dim">{label}</span>
      <div className="flex flex-wrap gap-2">
        {photoUrls.map((url, index) => (
          <button
            key={url}
            type="button"
            onClick={() => onOpen({ url, alt: photoAlt(index + 1) })}
            aria-label={
              highlight.has(url) && highlightLabel ? `${photoAlt(index + 1)}, ${highlightLabel}` : photoAlt(index + 1)
            }
            className="relative h-20 w-20 overflow-hidden rounded-md border-2"
            style={{
              borderColor: !highlight.has(url) ? "transparent" : highlightTone === "removed" ? "#ef4444" : "#16a34a",
            }}
          >
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={url}
              alt=""
              loading="lazy"
              decoding="async"
              className="h-full w-full object-cover"
              style={{ opacity: highlight.has(url) && highlightTone === "removed" ? 0.5 : 1 }}
            />
            {highlight.has(url) && highlightLabel ? (
              <span
                className="absolute inset-x-0 bottom-0 py-0.5 text-center text-[10px] font-semibold text-white"
                style={{ backgroundColor: highlightTone === "removed" ? "#b91c1c" : "#15803d" }}
              >
                {highlightLabel}
              </span>
            ) : null}
          </button>
        ))}
      </div>
    </div>
  );
}

function ChangedField({ label, from, to }: { label: string; from: string; to: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-dim">{label}</span>
      <div className="grid grid-cols-1 items-start gap-2 text-sm sm:grid-cols-[1fr_auto_1fr]">
        <p className="break-words text-dim line-through">{from}</p>
        <ArrowRight size={14} className="hidden text-dim sm:block" />
        <p className="font-medium break-words">{to}</p>
      </div>
    </div>
  );
}

export default function AdminSpotChangesPage() {
  const t = useTranslations("AdminSpotChanges");
  const tSpots = useTranslations("Spots");
  const locale = useLocale();

  const [actionError, setActionError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [rejectionReason, setRejectionReason] = useState("");
  const [lightboxPhoto, setLightboxPhoto] = useState<{ url: string; alt: string } | null>(null);

  const list = usePagedList<AdminSpotChangeRequest>(fetchChangeRequests);
  const { items, totalCount, status } = list;
  const error = actionError ?? (list.error ? getErrorMessage(list.error, t("unknownError")) : null);

  async function handleApprove(request: AdminSpotChangeRequest) {
    if (request.type === "Deletion" && !window.confirm(t("approveDeletionConfirm", { name: request.currentName })))
      return;

    setBusyId(request.id);
    try {
      await approveSpotChangeRequest(request.id);
      list.removeItem(request.id);
    } catch (err) {
      setActionError(getErrorMessage(err, t("unknownError")));
    } finally {
      setBusyId(null);
    }
  }

  async function handleReject(id: string) {
    setBusyId(id);
    try {
      await rejectSpotChangeRequest(id, rejectionReason.trim());
      list.removeItem(id);
      setRejectingId(null);
    } catch (err) {
      setActionError(getErrorMessage(err, t("unknownError")));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-5xl flex-1 flex-col gap-6 p-4 md:p-8">
      <div className="border-b border-contour pb-4">
        <h1 className="flex items-center gap-2 text-2xl font-semibold">
          <FilePenLine size={24} className="text-trail" />
          {t("title")}
          {status !== "loading" ? <span className="text-base font-normal text-dim">({totalCount})</span> : null}
        </h1>
        <p className="text-sm text-dim">{t("subtitle")}</p>
      </div>

      {error ? <ErrorMessage>{error}</ErrorMessage> : null}

      {status === "loading" ? (
        <StatusMessage>{t("loading")}</StatusMessage>
      ) : items.length === 0 ? (
        <p className="rounded-xl bg-card p-8 text-center text-sm text-dim">{t("empty")}</p>
      ) : (
        <ul className="flex flex-col gap-4">
          {items.map((request) => {
            const busy = busyId === request.id;
            const isDeletion = request.type === "Deletion";
            const proposedPhotos = request.proposedPhotoUrls ?? [];
            const photosChanged =
              !isDeletion &&
              (proposedPhotos.length !== request.currentPhotoUrls.length ||
                proposedPhotos.some((url, i) => url !== request.currentPhotoUrls[i]));
            const removedPhotos = new Set(request.currentPhotoUrls.filter((url) => !proposedPhotos.includes(url)));
            const addedPhotos = new Set(proposedPhotos.filter((url) => !request.currentPhotoUrls.includes(url)));

            return (
              <li
                key={request.id}
                className={`flex flex-col gap-4 rounded-xl border bg-card p-4 ${
                  isDeletion ? "border-red-500/35" : "border-contour"
                } ${busy ? "opacity-60" : ""}`}
              >
                <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
                  <div className="flex flex-wrap items-center gap-2">
                    {isDeletion ? (
                      <span className="flex items-center gap-1 rounded border border-red-300 bg-red-50 px-2 py-0.5 font-semibold text-red-700 dark:border-red-800 dark:bg-red-950 dark:text-red-400">
                        <Trash2 size={12} />
                        {t("typeDeletion")}
                      </span>
                    ) : (
                      <span className="flex items-center gap-1 rounded bg-contour px-2 py-0.5 font-semibold text-ink">
                        <FilePenLine size={12} />
                        {t("typeEdit")}
                      </span>
                    )}
                    <Link
                      href={`/spots/${request.spotId}`}
                      className="flex items-center gap-1 font-semibold hover:underline"
                    >
                      {request.currentName}
                      <ExternalLink size={12} />
                    </Link>
                    <span className="text-dim">• {formatRelativeTime(request.createdAt, locale)}</span>
                  </div>
                  <Link
                    href={`/admin/users/${request.requestedByUserId}`}
                    className="flex items-center gap-1 text-dim hover:underline"
                  >
                    <User size={12} />
                    {request.requestedByDisplayName}
                  </Link>
                </div>

                {isDeletion ? (
                  <div className="flex flex-col gap-3">
                    <p className="text-sm">{t("deletionDescription")}</p>
                    <PhotoStrip
                      photoUrls={request.currentPhotoUrls}
                      highlight={new Set()}
                      label={t("currentPhotosLabel")}
                      photoAlt={(number) => t("photoAlt", { number, spot: request.currentName })}
                      onOpen={setLightboxPhoto}
                    />
                  </div>
                ) : (
                  <div className="flex flex-col gap-3">
                    {request.proposedName !== request.currentName ? (
                      <ChangedField label={t("nameLabel")} from={request.currentName} to={request.proposedName ?? ""} />
                    ) : null}
                    {request.proposedDescription !== request.currentDescription ? (
                      <ChangedField
                        label={t("descriptionLabel")}
                        from={request.currentDescription}
                        to={request.proposedDescription ?? ""}
                      />
                    ) : null}
                    {request.proposedCategory !== request.currentCategory && request.proposedCategory ? (
                      <ChangedField
                        label={t("categoryLabel")}
                        from={tSpots(`category.${request.currentCategory}`)}
                        to={tSpots(`category.${request.proposedCategory}`)}
                      />
                    ) : null}
                    {photosChanged ? (
                      <div className="flex flex-col gap-2">
                        <span className="text-xs text-dim">{t("photosLabel")}</span>
                        <p className="text-sm font-medium">
                          {removedPhotos.size === 0 && addedPhotos.size === 0
                            ? t("photosReordered")
                            : t("photosSummary", { removed: removedPhotos.size, added: addedPhotos.size })}
                        </p>
                        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                          <PhotoStrip
                            photoUrls={request.currentPhotoUrls}
                            highlight={removedPhotos}
                            highlightLabel={t("photoRemovedBadge")}
                            highlightTone="removed"
                            label={t("currentPhotosLabel")}
                            photoAlt={(number) => t("photoAlt", { number, spot: request.currentName })}
                            onOpen={setLightboxPhoto}
                          />
                          <PhotoStrip
                            photoUrls={proposedPhotos}
                            highlight={addedPhotos}
                            highlightLabel={t("photoAddedBadge")}
                            highlightTone="added"
                            label={t("proposedPhotosLabel")}
                            photoAlt={(number) => t("photoAlt", { number, spot: request.currentName })}
                            onOpen={setLightboxPhoto}
                          />
                        </div>
                      </div>
                    ) : null}
                  </div>
                )}

                <div className="border-t border-contour pt-3">
                  {rejectingId === request.id ? (
                    <div className="flex flex-col gap-2">
                      <textarea
                        value={rejectionReason}
                        onChange={(e) => setRejectionReason(e.target.value)}
                        maxLength={MAX_REJECTION_REASON_LENGTH}
                        placeholder={t("rejectionReasonPlaceholder")}
                        rows={3}
                        autoFocus
                        className="rounded-lg border border-contour bg-paper-light px-3 py-2 text-sm text-ink"
                      />
                      <div className="flex flex-wrap justify-end gap-2 text-sm">
                        <button
                          onClick={() => setRejectingId(null)}
                          disabled={busy}
                          className="rounded-lg px-4 py-2 text-dim disabled:opacity-50"
                        >
                          {t("cancelButton")}
                        </button>
                        <button
                          onClick={() => handleReject(request.id)}
                          disabled={busy || rejectionReason.trim().length === 0}
                          className="rounded-lg bg-red-700 px-4 py-2 font-medium text-white hover:bg-red-600 disabled:opacity-50"
                        >
                          {t("confirmRejectButton")}
                        </button>
                      </div>
                    </div>
                  ) : (
                    <div className="flex flex-wrap justify-end gap-2 text-sm">
                      <button
                        onClick={() => {
                          setRejectingId(request.id);
                          setRejectionReason("");
                        }}
                        disabled={busy}
                        className="flex items-center gap-2 rounded-lg border border-red-300 bg-red-50 px-4 py-2 font-medium text-red-700 transition hover:bg-red-100 disabled:opacity-50 dark:border-red-800 dark:bg-red-950/40 dark:text-red-400 dark:hover:bg-red-950/70"
                      >
                        <X size={16} />
                        {t("rejectButton")}
                      </button>
                      <button
                        onClick={() => handleApprove(request)}
                        disabled={busy}
                        className="flex items-center gap-2 rounded-lg bg-ink px-4 py-2 font-medium text-paper-light shadow-sm transition hover:opacity-90 disabled:opacity-50"
                      >
                        <Check size={16} />
                        {isDeletion ? t("approveDeletionButton") : t("approveEditButton")}
                      </button>
                    </div>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <LoadMoreButton list={list} className="self-center" />

      {lightboxPhoto ? (
        <ImageLightbox src={lightboxPhoto.url} alt={lightboxPhoto.alt} onClose={() => setLightboxPhoto(null)} />
      ) : null}
    </div>
  );
}
