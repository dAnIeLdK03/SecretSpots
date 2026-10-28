"use client";

import { useTranslations, useLocale } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatRelativeTime } from "@/lib/relativeTime";
import type { MyRedemptionResponse } from "@/lib/rewardsApi";

export function RedemptionHistoryItem({ redemption }: { redemption: MyRedemptionResponse }) {
  const t = useTranslations("Rewards");
  const locale = useLocale();

  return (
    <li className="flex items-center justify-between gap-3 px-4 py-3 text-sm">
      <span className="flex flex-col">
        <Link href={`/businesses/${redemption.businessId}`} className="font-medium underline">
          {redemption.rewardTitle}
        </Link>
        <span className="text-xs text-dim">
          {redemption.businessName} · {formatRelativeTime(redemption.createdAt, locale)}
        </span>
      </span>
      <span className="whitespace-nowrap text-dim">-{redemption.crystalsSpent} {t("crystalsUnit")}</span>
    </li>
  );
}
