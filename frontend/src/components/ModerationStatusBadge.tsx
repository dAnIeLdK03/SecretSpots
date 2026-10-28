"use client";

import { useTranslations } from "next-intl";
import type { SpotModerationStatus } from "@/lib/spotsApi";

// The one place a spot's moderation status is drawn — admin moderation list, the admin user
// spots carousel and My spots all use this, so the colors/labels can't drift apart. Solid
// colors so it stays readable on top of a photo as well as on a card.
const STATUS_CLASSES: Record<SpotModerationStatus, string> = {
  Pending: "bg-contour text-ink",
  Approved: "bg-gold text-ink",
  Rejected: "bg-[#b91c1c] text-white",
};

interface ModerationStatusBadgeProps {
  status: SpotModerationStatus;
  // Positioning/spacing only (e.g. "absolute top-2 left-2", "self-start") — the look itself
  // isn't meant to be overridden per call site.
  className?: string;
}

export function ModerationStatusBadge({ status, className = "" }: ModerationStatusBadgeProps) {
  const t = useTranslations("Spots");

  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-semibold ${STATUS_CLASSES[status]} ${className}`}>
      {t(`moderationStatus.${status}`)}
    </span>
  );
}
