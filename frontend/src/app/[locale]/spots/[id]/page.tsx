"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations, useLocale } from "next-intl";
import { getSpot } from "@/lib/spotsApi";
import type { SpotResponse } from "@/lib/spotsApi";
import { ApiError, getErrorMessage } from "@/lib/apiClient";
import { formatRelativeTime } from "@/lib/relativeTime";

type LoadState =
  | { status: "loading" }
  | { status: "success"; spot: SpotResponse }
  | { status: "notFound" }
  | { status: "error"; message: string };

function SpotDetailContent({ id }: { id: string }) {
  const t = useTranslations("Spots");
  const locale = useLocale();
  const [state, setState] = useState<LoadState>({ status: "loading" });

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

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-1 flex-col gap-4 p-8">
      <img src={spot.photoUrl} alt={spot.name} className="h-64 w-full rounded-lg object-cover" />
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
    </div>
  );
}

export default function SpotDetailPage() {
  const { id } = useParams<{ id: string }>();
  return <SpotDetailContent key={id} id={id} />;
}
