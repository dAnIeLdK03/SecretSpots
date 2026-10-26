"use client";

import type { CSSProperties } from "react";
import { useTranslations } from "next-intl";
import type { SpotModerationStatus } from "@/lib/spotsApi";

// The one place a spot's moderation status is drawn — admin moderation list, the admin user
// spots carousel and My spots all use this, so the colors/labels can't drift apart. Solid
// colors so it stays readable on top of a photo as well as on a card.
const STATUS_STYLES: Record<SpotModerationStatus, CSSProperties> = {
  Pending: { backgroundColor: "var(--fieldmap-contour)", color: "var(--fieldmap-ink)" },
  Approved: { backgroundColor: "var(--fieldmap-gold)", color: "var(--fieldmap-ink)" },
  Rejected: { backgroundColor: "#b91c1c", color: "#fff" },
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
    <span className={`rounded-full px-2 py-0.5 text-xs font-semibold ${className}`} style={STATUS_STYLES[status]}>
      {t(`moderationStatus.${status}`)}
    </span>
  );
}
