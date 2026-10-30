"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useFormatter, useTranslations, useLocale } from "next-intl";
import { Clock, MapPin, Navigation, Tag } from "lucide-react";
import { DirectionsLinks } from "@/components/DirectionsLinks";
import { cancelSpotChangeRequest, deleteSpot, getMySpotChangeRequest, getSpot } from "@/lib/spotsApi";
import type { SpotChangeRequest, SpotResponse } from "@/lib/spotsApi";
import { ApiError, getErrorMessage } from "@/lib/apiClient";
import { formatRelativeTime } from "@/lib/relativeTime";
import { useAuthStore } from "@/store/useAuthStore";
import { Avatar } from "@/components/Avatar";
import { CheckInModal } from "@/components/CheckInModal";
import { CommentsSection } from "@/components/CommentsSection";
import { EditSpotModal } from "@/components/EditSpotModal";
import { PhotoSlider } from "@/components/PhotoSlider";
import { ReportModal } from "@/components/ReportModal";
import { reportSpot } from "@/lib/reportsApi";
import type { ReportReason } from "@/lib/reportsApi";
import { SaveSpotButton } from "@/components/SaveSpotButton";
import { SpotRatingInput } from "@/components/SpotRatingInput";
import { SpotRatingSummary } from "@/components/SpotRatingSummary";
import { Link } from "@/i18n/navigation";
import { CategoryIcon } from "@/components/CategoryIcon";
import { trackEvent } from "@/lib/analytics";

type LoadState =
  | { status: "loading" }
  | { status: "success"; spot: SpotResponse }
  | { status: "notFound" }
  | { status: "error"; message: string };

function SpotDetailContent({ id }: { id: string }) {
  const t = useTranslations("Spots");
  const tCheckIns = useTranslations("CheckIns");
  const tAuth = useTranslations("Auth");
  const locale = useLocale();
  const format = useFormatter();
  const user = useAuthStore((state) => state.user);
  const authStatus = useAuthStore((state) => state.status);
  const [state, setState] = useState<LoadState>({ status: "loading" });
  const [showCheckInModal, setShowCheckInModal] = useState(false);
  const [showLoginPrompt, setShowLoginPrompt] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showReportModal, setShowReportModal] = useState(false);
  const [showReportLoginPrompt, setShowReportLoginPrompt] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [cancellingRequest, setCancellingRequest] = useState(false);
  const [changeRequest, setChangeRequest] = useState<SpotChangeRequest | null>(null);
  const [error, setError] = useState<string | null>(null);

  const ownerId = state.status === "success" ? state.spot.createdByUserId : null;
  const viewerIsOwner = authStatus === "authenticated" && ownerId !== null && user?.id === ownerId;

  useEffect(() => {
    const controller = new AbortController();

    getSpot(id, controller.signal)
      .then((spot) => {
        setState({ status: "success", spot });
        trackEvent("spot_view", { spotId: spot.id, category: spot.category });
      })
      .catch((err) => {
        if (controller.signal.aborted) return;
        if (err instanceof ApiError && err.status === 404) {
          setState({ status: "notFound" });
        } else {
          setState({ status: "error", message: getErrorMessage(err, t("unknownError")) });
        }
      });

    return () => controller.abort();
  }, [id, t]);

  // Only the creator has change requests to see; a 404 just means there's never been one.
  useEffect(() => {
    if (!viewerIsOwner) return;
    const controller = new AbortController();

    getMySpotChangeRequest(id, controller.signal)
      .then(setChangeRequest)
      .catch(() => {
        if (!controller.signal.aborted) setChangeRequest(null);
      });

    return () => controller.abort();
  }, [id, viewerIsOwner]);

  if (state.status === "loading") {
    return (
      <div className="flex flex-1 items-center justify-center p-8">
        <p className="text-sm text-dim">{t("loading")}</p>
      </div>
    );
  }

  if (state.status === "notFound") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-8 text-center">
        <h1 className="text-xl font-semibold">{t("notFoundTitle")}</h1>
        <p className="text-sm text-dim">{t("notFoundMessage")}</p>
      </div>
    );
  }

  if (state.status === "error") {
    return (
      <div className="flex flex-1 items-center justify-center p-8">
        <p className="text-sm text-red-700 dark:text-red-400">{state.message}</p>
      </div>
    );
  }

  const { spot } = state;
  const isOwner = authStatus === "authenticated" && user?.id === spot.createdByUserId;

  async function handleDeleteClick() {
    if (!window.confirm(t("deleteConfirm"))) {
      return;
    }

    setDeleting(true);
    setError(null);
    try {
      setChangeRequest(await deleteSpot(spot.id));
      trackEvent("spot_deletion_requested", { spotId: spot.id });
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setDeleting(false);
    }
  }

  async function handleCancelChangeRequest() {
    setCancellingRequest(true);
    setError(null);
    try {
      await cancelSpotChangeRequest(spot.id);
      setChangeRequest(null);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setCancellingRequest(false);
    }
  }

  const pendingRequest = changeRequest?.status === "Pending" ? changeRequest : null;

  function handleCheckInClick() {
    if (authStatus !== "authenticated") {
      setShowLoginPrompt(true);
      return;
    }
    setShowLoginPrompt(false);
    setShowCheckInModal(true);
  }

  function handleReportClick() {
    if (authStatus !== "authenticated") {
      setShowReportLoginPrompt(true);
      return;
    }
    setShowReportLoginPrompt(false);
    setShowReportModal(true);
  }

  async function handleReportSubmit(reason: ReportReason, details: string) {
    await reportSpot(spot.id, reason, details);
    trackEvent("report_submitted", { contentType: "spot", spotId: spot.id, reason });
  }

  return (
    <div className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-4 p-8">
      <div className="flex flex-col gap-4 rounded-2xl bg-card p-4 shadow-sm md:flex-row">
        <div className="md:w-96 md:flex-shrink-0">
          <PhotoSlider photos={spot.photoUrls} alt={spot.name} />
        </div>

        <div className="flex flex-1 flex-col gap-3">
          {/* Only the creator can load a Rejected spot at all (GetSpot 404s for everyone else). */}
          {isOwner && spot.moderationStatus === "Rejected" ? (
            <div className="rounded border border-red-300 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-800 dark:bg-red-950 dark:text-red-400">
              <p className="font-medium">{t("rejectedNotice")}</p>
              {spot.rejectionReason ? (
                <p>
                  {t("rejectionReasonLabel")}: {spot.rejectionReason}
                </p>
              ) : null}
              {spot.scheduledDeletionAt ? (
                <p>
                  {t("scheduledDeletionNotice", {
                    date: format.dateTime(new Date(spot.scheduledDeletionAt), {
                      dateStyle: "medium",
                      timeStyle: "short",
                    }),
                  })}
                </p>
              ) : null}
            </div>
          ) : null}
          {isOwner && pendingRequest ? (
            <div className="flex flex-wrap items-center justify-between gap-2 rounded border border-contour bg-paper-light px-3 py-2 text-sm text-ink">
              <p>{pendingRequest.type === "Deletion" ? t("deletionPendingNotice") : t("editPendingNotice")}</p>
              <button
                onClick={handleCancelChangeRequest}
                disabled={cancellingRequest}
                className="text-sm underline disabled:opacity-50"
              >
                {t("cancelChangeRequestButton")}
              </button>
            </div>
          ) : null}
          {isOwner && changeRequest?.status === "Rejected" ? (
            <div className="rounded border border-red-300 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-800 dark:bg-red-950 dark:text-red-400">
              <p className="font-medium">
                {changeRequest.type === "Deletion" ? t("deletionRejectedNotice") : t("editRejectedNotice")}
              </p>
              {changeRequest.rejectionReason ? (
                <p>
                  {t("rejectionReasonLabel")}: {changeRequest.rejectionReason}
                </p>
              ) : null}
            </div>
          ) : null}
          <h1 className="text-2xl font-semibold">{spot.name}</h1>
          <SpotRatingSummary averageRating={spot.averageRating} ratingsCount={spot.ratingsCount} />

          <div>
            <h2 className="text-xs font-semibold tracking-wide text-dim uppercase">{t("descriptionLabel")}</h2>
            <p className="text-sm text-ink">{spot.description}</p>
          </div>

          <dl className="grid grid-cols-[auto_1fr] items-center gap-x-3 gap-y-2 border-t border-contour pt-3 text-sm">
            <dt className="flex items-center gap-2 text-dim">
              <Tag size={16} />
              {t("categoryLabel")}
            </dt>
            <dd className="flex items-center gap-1.5">
              <CategoryIcon category={spot.category} size={14} />
              {t(`category.${spot.category}`)}
            </dd>

            <dt className="flex items-center gap-2 text-dim">
              <Avatar name={spot.createdByDisplayName} size={20} />
              {t("authorLabel")}
            </dt>
            <dd>@{spot.createdByDisplayName}</dd>

            <dt className="flex items-center gap-2 text-dim">
              <Clock size={16} />
              {t("createdAtLabel")}
            </dt>
            <dd>{formatRelativeTime(spot.createdAt, locale)}</dd>

            <dt className="flex items-center gap-2 text-dim">
              <MapPin size={16} />
              {t("locationLabel")}
            </dt>
            <dd className="flex flex-wrap items-center gap-x-2">
              <span>
                {spot.latitude.toFixed(5)}, {spot.longitude.toFixed(5)}
              </span>
              <Link
                href={`/map?lat=${spot.latitude}&lng=${spot.longitude}&spot=${spot.id}`}
                className="text-trail underline"
              >
                {t("viewOnMap")}
              </Link>
            </dd>

            <dt className="flex items-center gap-2 text-dim">
              <Navigation size={16} />
              {t("directionsLabel")}
            </dt>
            <dd>
              <DirectionsLinks latitude={spot.latitude} longitude={spot.longitude} />
            </dd>
          </dl>

          <div>
            <div className="flex flex-wrap items-center gap-2">
              {!isOwner && (
                <button onClick={handleCheckInClick} className="rounded bg-trail px-4 py-2 text-sm text-paper-light">
                  {tCheckIns("checkInButton")}
                </button>
              )}
              <SaveSpotButton spotId={spot.id} />
              {!isOwner && (
                <button
                  onClick={handleReportClick}
                  className="rounded border border-contour px-4 py-2 text-sm text-dim"
                >
                  {t("reportButton")}
                </button>
              )}

              {isOwner && (
                <div className="ml-auto flex items-center gap-2">
                  <button
                    onClick={() => setShowEditModal(true)}
                    className="rounded border border-contour px-4 py-2 text-sm"
                  >
                    {t("editButton")}
                  </button>
                  <button
                    onClick={handleDeleteClick}
                    disabled={deleting || pendingRequest?.type === "Deletion"}
                    className="rounded border border-red-300 px-4 py-2 text-sm text-red-700 disabled:opacity-50 dark:border-red-800 dark:text-red-400"
                  >
                    {t("deleteButton")}
                  </button>
                </div>
              )}
            </div>
            {error ? <p className="mt-2 text-sm text-red-700 dark:text-red-400">{error}</p> : null}
            {showLoginPrompt ? (
              <p className="mt-2 text-sm text-dim">
                {tCheckIns("loginRequiredToCheckIn")}{" "}
                <Link href="/login" className="underline">
                  {tAuth("loginTitle")}
                </Link>
              </p>
            ) : null}
            {showReportLoginPrompt ? (
              <p className="mt-2 text-sm text-dim">
                {t("loginRequiredToReport")}{" "}
                <Link href="/login" className="underline">
                  {tAuth("loginTitle")}
                </Link>
              </p>
            ) : null}
          </div>
        </div>
      </div>

      {!isOwner && (
        <SpotRatingInput
          spotId={spot.id}
          onRated={(stats) => setState({ status: "success", spot: { ...spot, ...stats } })}
        />
      )}

      <CommentsSection spotId={spot.id} />

      {showCheckInModal ? <CheckInModal spotId={spot.id} onClose={() => setShowCheckInModal(false)} /> : null}
      {showReportModal ? <ReportModal onClose={() => setShowReportModal(false)} onSubmit={handleReportSubmit} /> : null}
      {showEditModal ? (
        <EditSpotModal
          spot={spot}
          onClose={() => setShowEditModal(false)}
          onSubmitted={(request) => {
            setChangeRequest(request);
            setShowEditModal(false);
          }}
        />
      ) : null}
    </div>
  );
}

export default function SpotDetailPage() {
  const { id } = useParams<{ id: string }>();
  return <SpotDetailContent key={id} id={id} />;
}
