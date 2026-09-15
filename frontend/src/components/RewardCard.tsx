"use client";

import { useState } from "react";
import { Gem, Check } from "lucide-react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/store/useAuthStore";
import { redeemReward } from "@/lib/rewardsApi";
import type { RewardResponse } from "@/lib/rewardsApi";
import { getErrorMessage } from "@/lib/apiClient";
import { Link } from "@/i18n/navigation";

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
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setRedeeming(false);
    }
  }

  return (
    <li
      className="flex flex-col gap-2 rounded-md border p-4"
      style={{ borderColor: "var(--fieldmap-contour)" }}
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="font-medium">{reward.title}</h3>
          <p className="mt-1 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
            {reward.description}
          </p>
        </div>
        <span
          className="inline-flex shrink-0 items-center gap-1 whitespace-nowrap rounded-full px-3 py-1 text-sm font-medium"
          style={{ backgroundColor: "var(--fieldmap-card)", color: "var(--fieldmap-ink)" }}
        >
          <Gem size={14} />
          {reward.crystalCost}
        </span>
      </div>

      {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}

      {!isAuthenticated ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loginRequiredToRedeem")}{" "}
          <Link href="/login" className="underline">
            {t("loginLink")}
          </Link>
        </p>
      ) : redeemed ? (
        <span className="inline-flex items-center gap-1.5 text-sm font-medium" style={{ color: "var(--fieldmap-trail)" }}>
          <Check size={16} />
          {t("redeemedLabel")}
        </span>
      ) : (
        <button
          type="button"
          onClick={handleRedeem}
          disabled={redeeming || !canAfford}
          className="self-start rounded px-4 py-2 text-sm disabled:opacity-50"
          style={{ backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper-light)" }}
        >
          {redeeming ? t("redeeming") : canAfford ? t("redeemButton") : t("insufficientBalance")}
        </button>
      )}
    </li>
  );
}
