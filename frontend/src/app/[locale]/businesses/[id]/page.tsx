"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { getBusiness } from "@/lib/businessesApi";
import type { BusinessResponse } from "@/lib/businessesApi";
import { getBusinessRewards } from "@/lib/rewardsApi";
import type { RewardResponse } from "@/lib/rewardsApi";
import { ApiError, getErrorMessage } from "@/lib/apiClient";
import { RewardCard } from "@/components/RewardCard";

type LoadState =
  | { status: "loading" }
  | { status: "success"; business: BusinessResponse; rewards: RewardResponse[] }
  | { status: "notFound" }
  | { status: "error"; message: string };

function BusinessDetailContent({ id }: { id: string }) {
  const t = useTranslations("Businesses");
  const [state, setState] = useState<LoadState>({ status: "loading" });

  useEffect(() => {
    const controller = new AbortController();

    Promise.all([getBusiness(id, controller.signal), getBusinessRewards(id, controller.signal)])
      .then(([business, rewards]) => setState({ status: "success", business, rewards }))
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
      <p className="p-8 text-center text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {t("loading")}
      </p>
    );
  }

  if (state.status === "notFound") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-8 text-center">
        <h1 className="text-xl font-semibold">{t("notFoundTitle")}</h1>
        <p style={{ color: "var(--fieldmap-dim)" }}>{t("notFoundMessage")}</p>
      </div>
    );
  }

  if (state.status === "error") {
    return (
      <p className="p-8 text-center text-sm text-red-700 dark:text-red-400">{state.message}</p>
    );
  }

  const { business, rewards } = state;

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-1 flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">{business.name}</h1>
        <p className="mt-2 whitespace-pre-wrap text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {business.description}
        </p>
      </div>

      <div>
        <h2 className="mb-3 text-lg font-semibold">{t("rewardsTitle")}</h2>
        {rewards.length === 0 ? (
          <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
            {t("noRewardsYet")}
          </p>
        ) : (
          <ul className="flex flex-col gap-3">
            {rewards.map((reward) => (
              <RewardCard key={reward.id} reward={reward} />
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

export default function BusinessDetailPage() {
  const params = useParams<{ id: string }>();
  return <BusinessDetailContent id={params.id} />;
}
