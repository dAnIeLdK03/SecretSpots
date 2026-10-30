"use client";

import { useState } from "react";
import { Gem, Check } from "lucide-react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/store/useAuthStore";
import { redeemReward } from "@/lib/rewardsApi";
import type { RewardResponse } from "@/lib/rewardsApi";
import { getErrorMessage } from "@/lib/apiClient";
import { Link } from "@/i18n/navigation";
import { trackEvent } from "@/lib/analytics";

export function RewardCard({ reward }: { reward: RewardResponse }) {
  const t = useTranslations("Rewards");
  const authStatus = useAuthStore((state) => state.status);
  const user = useAuthStore((state) => state.user);
  const setCrystalBalance = useAuthStore((state) => state.setCrystalBalance);

  const [redeeming, setRedeeming] = useState(false);
  const [redeemed, setRedeemed] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isAuthenticated = authStatus === "authenticated" && user !== null;
  const canAfford = isAuthenticated && user.crystalBalance >= reward.crystalCost;

  async function handleRedeem() {
    if (redeeming || redeemed) return;

    setRedeeming(true);
    setError(null);
    try {
      const result = await redeemReward(reward.id);
      setCrystalBalance(result.newCrystalBalance);
      setRedeemed(true);
      trackEvent("reward_redeemed", { rewardId: reward.id, crystalCost: reward.crystalCost });
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setRedeeming(false);
    }
  }

  return (
    <li className={`flex flex-col gap-2 rounded-md border p-4 ${reward.isActive ? "" : "opacity-60"} border-contour`}>
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="font-medium">{reward.title}</h3>
          <p className="mt-1 text-sm text-dim">{reward.description}</p>
        </div>
        <span className="inline-flex shrink-0 items-center gap-1 rounded-full bg-card px-3 py-1 text-sm font-medium whitespace-nowrap text-ink">
          <Gem size={14} />
          {reward.crystalCost}
        </span>
      </div>

      {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}

      {!reward.isActive ? (
        <span className="text-sm font-medium text-dim">{t("rewardUnavailable")}</span>
      ) : !isAuthenticated ? (
        <p className="text-sm text-dim">
          {t("loginRequiredToRedeem")}{" "}
          <Link href="/login" className="underline">
            {t("loginLink")}
          </Link>
        </p>
      ) : redeemed ? (
        <span className="inline-flex items-center gap-1.5 text-sm font-medium text-link">
          <Check size={16} />
          {t("redeemedLabel")}
        </span>
      ) : (
        <button
          type="button"
          onClick={handleRedeem}
          disabled={redeeming || !canAfford}
          className="self-start rounded bg-ink px-4 py-2 text-sm text-paper-light disabled:opacity-50"
        >
          {redeeming ? t("redeeming") : canAfford ? t("redeemButton") : t("insufficientBalance")}
        </button>
      )}
    </li>
  );
}
