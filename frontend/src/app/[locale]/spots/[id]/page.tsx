"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations, useLocale } from "next-intl";
import { deleteSpot, getSpot } from "@/lib/spotsApi";
import type { SpotResponse } from "@/lib/spotsApi";
import { ApiError, getErrorMessage } from "@/lib/apiClient";
import { formatRelativeTime } from "@/lib/relativeTime";
import { useAuthStore } from "@/store/useAuthStore";
import { CheckInModal } from "@/components/CheckInModal";
import { EditSpotModal } from "@/components/EditSpotModal";
import { PhotoSlider } from "@/components/PhotoSlider";
import { Link, useRouter } from "@/i18n/navigation";

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
        <p className="text-sm text-zinc-600 dark:text-zinc-400">{t("loading")}</p>
      </div>
    );
  }

  if (state.status === "notFound") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-8 text-center">
        <h1 className="text-xl font-semibold">{t("notFoundTitle")}</h1>
        <p className="text-sm text-zinc-600 dark:text-zinc-400">{t("notFoundMessage")}</p>
      </div>
    );
  }

  if (state.status === "error") {
    return (
      <div className="flex flex-1 items-center justify-center p-8">
        <p className="text-sm text-red-600 dark:text-red-400">{state.message}</p>
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
      router.push("/");
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
    <div className="mx-auto flex w-full max-w-2xl flex-1 flex-col gap-4 p-8">
      <PhotoSlider photos={spot.photoUrls} alt={spot.name} />
      <h1 className="text-2xl font-semibold">{spot.name}</h1>
      <p className="text-sm text-zinc-700 dark:text-zinc-300">{spot.description}</p>
      <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
        <dt className="text-zinc-600 dark:text-zinc-400">{t("categoryLabel")}</dt>
        <dd>{t(`category.${spot.category}`)}</dd>
        <dt className="text-zinc-600 dark:text-zinc-400">{t("authorLabel")}</dt>
        <dd>{spot.createdByUserId}</dd>
        <dt className="text-zinc-600 dark:text-zinc-400">{t("createdAtLabel")}</dt>
        <dd>{formatRelativeTime(spot.createdAt, locale)}</dd>
      </dl>

      <div>
        <button
          onClick={handleCheckInClick}
          className="rounded bg-zinc-900 px-4 py-2 text-sm text-white dark:bg-zinc-100 dark:text-zinc-900"
        >
          {tCheckIns("checkInButton")}
        </button>
        {isOwner && (
          <>
            <button
              onClick={() => setShowEditModal(true)}
              className="ml-2 rounded border border-zinc-300 px-4 py-2 text-sm dark:border-zinc-700"
            >
              {t("editButton")}
            </button>
            <button
              onClick={handleDeleteClick}
              disabled={deleting}
              className="ml-2 rounded border border-red-300 px-4 py-2 text-sm text-red-600 disabled:opacity-50 dark:border-red-900 dark:text-red-400"
            >
              {t("deleteButton")}
            </button>
          </>
        )}
        {error ? <p className="mt-2 text-sm text-red-600 dark:text-red-400">{error}</p> : null}
        {showLoginPrompt ? (
          <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
            {tCheckIns("loginRequiredToCheckIn")}{" "}
            <Link href="/login" className="underline">
              {tAuth("loginTitle")}
            </Link>
          </p>
        ) : null}
      </div>

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
