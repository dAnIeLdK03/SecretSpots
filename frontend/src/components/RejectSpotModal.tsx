"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { CircleAlert, X } from "lucide-react";
import { useModalDismiss } from "@/hooks/useModalDismiss";
import { MAX_REJECTION_REASON_LENGTH } from "@/lib/adminApi";

const DETAILS_SEPARATOR = " — ";

const PRESET_REASONS = ["inappropriate", "lowQualityPhoto", "spam", "duplicate", "other"] as const;
type PresetReason = (typeof PRESET_REASONS)[number];

interface RejectSpotModalProps {
  spotName: string;
  // Rejecting an already Approved spot — same action, worded as taking it down.
  isRemoval: boolean;
  busy: boolean;
  onConfirm: (reason: string) => void;
  onClose: () => void;
}

export function RejectSpotModal({ spotName, isRemoval, busy, onConfirm, onClose }: RejectSpotModalProps) {
  const t = useTranslations("AdminSpots");
  const { containerRef, handleOverlayClick } = useModalDismiss(onClose);
  const [preset, setPreset] = useState<PresetReason>("inappropriate");
  const [details, setDetails] = useState("");

  const isOther = preset === "other";
  const title = isRemoval ? t("removeModalTitle") : t("rejectModalTitle");
  const presetLabel = isOther ? "" : t(`rejectReasons.${preset}`);
  // The creator sees one plain-text reason, so a preset and its optional details are joined.
  const detailsMaxLength = isOther
    ? MAX_REJECTION_REASON_LENGTH
    : MAX_REJECTION_REASON_LENGTH - presetLabel.length - DETAILS_SEPARATOR.length;
  const trimmedDetails = details.trim();
  const reason = isOther
    ? trimmedDetails
    : trimmedDetails
      ? `${presetLabel}${DETAILS_SEPARATOR}${trimmedDetails}`
      : presetLabel;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (reason.length === 0) return;
    onConfirm(reason);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4" onClick={handleOverlayClick}>
      <div
        ref={containerRef}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        tabIndex={-1}
        className="w-full max-w-md rounded-xl p-6 shadow-2xl outline-none bg-paper-light text-ink"
      >
        <div className="mb-3 flex items-center justify-between gap-2 border-b pb-3 border-contour">
          <h2 className="flex items-center gap-2 text-lg font-semibold">
            <CircleAlert size={18} className="text-red-700 dark:text-red-400" />
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label={t("closeLabel")}
            className="rounded-full p-1 hover:bg-black/5 dark:hover:bg-white/5 text-dim"
          >
            <X size={18} />
          </button>
        </div>

        <p className="mb-4 text-sm text-dim">
          {t(isRemoval ? "removeModalDescription" : "rejectModalDescription", { name: spotName })}
        </p>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <fieldset className="flex flex-col gap-2">
            {PRESET_REASONS.map((value) => (
              <label
                key={value}
                className={`flex cursor-pointer items-center gap-2 rounded-lg border bg-card px-3 py-2.5 text-sm ${
                  preset === value ? "border-ink" : "border-contour"
                }`}
              >
                <input
                  type="radio"
                  name="reject_reason"
                  value={value}
                  checked={preset === value}
                  onChange={() => setPreset(value)}
                />
                {t(`rejectReasons.${value}`)}
              </label>
            ))}
          </fieldset>

          <div className="flex flex-col gap-1">
            <textarea
              value={details}
              onChange={(e) => setDetails(e.target.value)}
              maxLength={detailsMaxLength}
              placeholder={isOther ? t("rejectionReasonPlaceholder") : t("rejectionDetailsPlaceholder")}
              rows={3}
              className="rounded-lg border px-3 py-2 text-sm border-contour bg-card text-ink"
            />
            <span className="self-end text-xs text-dim">{details.length}/{detailsMaxLength}</span>
          </div>

          <div className="flex gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={busy}
              className="flex-1 rounded-lg border px-4 py-2 text-sm font-medium disabled:opacity-50 border-contour"
            >
              {t("cancelButton")}
            </button>
            <button
              type="submit"
              disabled={busy || reason.length === 0}
              className="flex-1 rounded-lg bg-red-700 px-4 py-2 text-sm font-medium text-white hover:bg-red-600 disabled:opacity-50"
            >
              {isRemoval ? t("confirmRemoveButton") : t("confirmRejectButton")}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
