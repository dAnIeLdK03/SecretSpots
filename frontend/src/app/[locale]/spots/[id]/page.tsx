"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations, useLocale } from "next-intl";
import { Clock, Tag } from "lucide-react";
import { deleteSpot, getSpot } from "@/lib/spotsApi";
import type { SpotResponse } from "@/lib/spotsApi";
import { ApiError, getErrorMessage } from "@/lib/apiClient";
import { formatRelativeTime } from "@/lib/relativeTime";
import { useAuthStore } from "@/store/useAuthStore";
import { Avatar } from "@/components/Avatar";
import { CheckInModal } from "@/components/CheckInModal";
import { CommentsSection } from "@/components/CommentsSection";
import { EditSpotModal } from "@/components/EditSpotModal";
import { PhotoSlider } from "@/components/PhotoSlider";
import { SaveSpotButton } from "@/components/SaveSpotButton";
import { SpotRatingInput } from "@/components/SpotRatingInput";
import { SpotRatingSummary } from "@/components/SpotRatingSummary";
import { Link, useRouter } from "@/i18n/navigation";
import { CategoryIcon } from "@/components/CategoryIcon";

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
  const router = useRouter();
  const user = useAuthStore((state) => state.user);
  const authStatus = useAuthStore((state) => state.status);
  const [state, setState] = useState<LoadState>({ status: "loading" });
  const [showCheckInModal, setShowCheckInModal] = useState(false);
  const [showLoginPrompt, setShowLoginPrompt] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    getSpot(id, controller.signal)
      .then((spot) => setState({ status: "success", spot }))
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

  if (state.status === "loading") {
    return (
      <div className="flex flex-1 items-center justify-center p-8">
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loading")}
        </p>
      </div>
    );
  }

  if (state.status === "notFound") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-8 text-center">
        <h1 className="text-xl font-semibold">{t("notFoundTitle")}</h1>
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("notFoundMessage")}
        </p>
      </div>
    );
  }

  if (state.status === "error") {
    return (
      <div className="flex flex-1 items-center justify-center p-8">
        <p className="text-sm text-red-700">{state.message}</p>
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
      await deleteSpot(spot.id);
      router.push("/map");
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
      setDeleting(false);
    }
  }

  function handleCheckInClick() {
    if (authStatus !== "authenticated") {
      setShowLoginPrompt(true);
      return;
    }
    setShowLoginPrompt(false);
    setShowCheckInModal(true);
  }

  return (
    <div className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-4 p-8">
      <div
        className="flex flex-col gap-4 rounded-2xl p-4 shadow-sm md:flex-row"
        style={{ backgroundColor: "var(--fieldmap-card)" }}
      >
        <div className="md:w-96 md:flex-shrink-0">
          <PhotoSlider photos={spot.photoUrls} alt={spot.name} />
        </div>

        <div className="flex flex-1 flex-col gap-3">
          <h1 className="text-2xl font-semibold">{spot.name}</h1>
          <SpotRatingSummary averageRating={spot.averageRating} ratingsCount={spot.ratingsCount} />

          <div>
            <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: "var(--fieldmap-dim)" }}>
              {t("descriptionLabel")}
            </h2>
            <p className="text-sm" style={{ color: "var(--fieldmap-ink)" }}>
              {spot.description}
            </p>
          </div>

          <dl
            className="grid grid-cols-[auto_1fr] items-center gap-x-3 gap-y-2 border-t pt-3 text-sm"
            style={{ borderColor: "var(--fieldmap-contour)" }}
          >
            <dt className="flex items-center gap-2" style={{ color: "var(--fieldmap-dim)" }}>
              <Tag size={16} />
              {t("categoryLabel")}
            </dt>
            <dd className="flex items-center gap-1.5">
              <CategoryIcon category={spot.category} size={14} />
              {t(`category.${spot.category}`)}
            </dd>

            <dt className="flex items-center gap-2" style={{ color: "var(--fieldmap-dim)" }}>
              <Avatar name={spot.createdByDisplayName} size={20} />
              {t("authorLabel")}
            </dt>
            <dd>@{spot.createdByDisplayName}</dd>

            <dt className="flex items-center gap-2" style={{ color: "var(--fieldmap-dim)" }}>
              <Clock size={16} />
              {t("createdAtLabel")}
            </dt>
            <dd>{formatRelativeTime(spot.createdAt, locale)}</dd>
          </dl>

          <div>
            <div className="flex flex-wrap items-center gap-2">
              <button
                onClick={handleCheckInClick}
                className="rounded px-4 py-2 text-sm"
                style={{ backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }}
              >
                {tCheckIns("checkInButton")}
              </button>
              <SaveSpotButton spotId={spot.id} />

              {isOwner && (
                <div className="ml-auto flex items-center gap-2">
                  <button
                    onClick={() => setShowEditModal(true)}
                    className="rounded border px-4 py-2 text-sm"
                    style={{ borderColor: "var(--fieldmap-contour)" }}
                  >
                    {t("editButton")}
                  </button>
                  <button
                    onClick={handleDeleteClick}
                    disabled={deleting}
                    className="rounded border border-red-300 px-4 py-2 text-sm text-red-700 disabled:opacity-50"
                  >
                    {t("deleteButton")}
                  </button>
                </div>
              )}
            </div>
            {error ? <p className="mt-2 text-sm text-red-700">{error}</p> : null}
            {showLoginPrompt ? (
              <p className="mt-2 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
                {tCheckIns("loginRequiredToCheckIn")}{" "}
                <Link href="/login" className="underline">
                  {tAuth("loginTitle")}
                </Link>
              </p>
            ) : null}
          </div>
        </div>
      </div>

      <SpotRatingInput
        spotId={spot.id}
        onRated={(stats) => setState({ status: "success", spot: { ...spot, ...stats } })}
      />

      <CommentsSection spotId={spot.id} />

      {showCheckInModal ? <CheckInModal spotId={spot.id} onClose={() => setShowCheckInModal(false)} /> : null}
      {showEditModal ? (
        <EditSpotModal
          spot={spot}
          onClose={() => setShowEditModal(false)}
          onUpdate={(updated) => {
            setState({ status: "success", spot: updated });
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
