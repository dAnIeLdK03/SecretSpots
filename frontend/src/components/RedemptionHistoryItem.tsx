"use client";

import { useTranslations, useLocale } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatRelativeTime } from "@/lib/relativeTime";
import type { MyRedemptionResponse } from "@/lib/rewardsApi";

export function RedemptionHistoryItem({ redemption }: { redemption: MyRedemptionResponse }) {
  const t = useTranslations("Rewards");
  const locale = useLocale();

  return (
    <li className="flex flex-col gap-2 px-4 py-3 text-sm">
      <div className="flex items-center justify-between gap-3">
        <span className="flex flex-col">
          <Link href={`/businesses/${redemption.businessId}`} className="font-medium underline">
            {redemption.rewardTitle}
          </Link>
          <span className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
            {redemption.businessName} · {formatRelativeTime(redemption.createdAt, locale)}
          </span>
        </span>
        <span className="whitespace-nowrap" style={{ color: "var(--fieldmap-dim)" }}>
          -{redemption.crystalsSpent} {t("crystalsUnit")}
        </span>
      </div>

      <div className="flex items-center justify-between gap-3">
        <span
          className="rounded px-2 py-1 font-mono text-xs font-semibold tracking-widest"
          style={{ backgroundColor: "var(--fieldmap-card)", color: "var(--fieldmap-ink)" }}
        >
          {redemption.redemptionCode}
        </span>
        <span
          className="text-xs font-medium"
          style={{ color: redemption.isFulfilled ? "var(--fieldmap-trail)" : "var(--fieldmap-dim)" }}
        >
          {redemption.isFulfilled ? t("fulfilledLabel") : t("notFulfilledYetLabel")}
        </span>
      </div>
    </li>
  );
}
