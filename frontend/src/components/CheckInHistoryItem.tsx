"use client";

import { useTranslations, useLocale } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatRelativeTime } from "@/lib/relativeTime";
import type { MyCheckInResponse } from "@/lib/checkInsApi";

export function CheckInHistoryItem({ checkIn }: { checkIn: MyCheckInResponse }) {
  const t = useTranslations("CheckInsHistory");
  const locale = useLocale();

  return (
    <li className="flex items-center justify-between gap-3 px-4 py-3 text-sm">
      <span className="flex flex-col">
        <Link href={`/spots/${checkIn.spotId}`} className="font-medium underline">
          {checkIn.spotName}
        </Link>
        <span className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
          {formatRelativeTime(checkIn.createdAt, locale)}
        </span>
      </span>
      <span className="whitespace-nowrap" style={{ color: "var(--fieldmap-dim)" }}>
        +{checkIn.crystalsAwarded} {t("crystalsUnit")}
      </span>
    </li>
  );
}
