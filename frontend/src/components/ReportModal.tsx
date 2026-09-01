"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { getErrorMessage, ApiError } from "@/lib/apiClient";
import { REPORT_REASONS } from "@/lib/reportsApi";
import type { ReportReason } from "@/lib/reportsApi";

const MAX_DETAILS_LENGTH = 500;

interface ReportModalProps {
  onClose: () => void;
  onSubmit: (reason: ReportReason, details: string) => Promise<void>;
}

export function ReportModal({ onClose, onSubmit }: ReportModalProps) {
  const t = useTranslations("Reports");
  const [reason, setReason] = useState<ReportReason>(REPORT_REASONS[0]);
  const [details, setDetails] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await onSubmit(reason, details.trim());
      setSubmitted(true);
    } catch (err) {
      const fallback = err instanceof ApiError && err.status === 409 ? t("alreadyReported") : t("unknownError");
      setError(getErrorMessage(err, fallback));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div
        className="w-full max-w-sm rounded-lg p-6"
        style={{ backgroundColor: "var(--fieldmap-paper-light)", color: "var(--fieldmap-ink)" }}
      >
        {submitted ? (
          <>
            <p className="text-sm">{t("submittedMessage")}</p>
            <div className="mt-4 flex justify-end">
              <button
                type="button"
                onClick={onClose}
                className="rounded px-4 py-2 text-sm"
                style={{ backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }}
              >
                {t("closeButton")}
              </button>
            </div>
          </>
        ) : (
          <>
            <h2 className="mb-4 text-lg font-semibold">{t("modalTitle")}</h2>
            <form onSubmit={handleSubmit} className="flex flex-col gap-4">
              <fieldset className="flex flex-col gap-2">
                <legend className="mb-1 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
                  {t("reasonLabel")}
                </legend>
                {REPORT_REASONS.map((r) => (
                  <label key={r} className="flex items-center gap-2 text-sm">
                    <input type="radio" name="reason" value={r} checked={reason === r} onChange={() => setReason(r)} />
                    {t(`reason.${r}`)}
                  </label>
                ))}
              </fieldset>
              <label className="flex flex-col gap-1">
                <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
                  {t("detailsLabel")}
                </span>
                <textarea
                  value={details}
                  onChange={(e) => setDetails(e.target.value)}
                  maxLength={MAX_DETAILS_LENGTH}
                  rows={3}
                  className="rounded border px-3 py-2 text-sm"
                  style={{ borderColor: "var(--fieldmap-contour)" }}
                />
              </label>
              {error ? <p className="text-sm text-red-700">{error}</p> : null}
              <div className="flex justify-end gap-2">
                <button type="button" onClick={onClose} style={{ color: "var(--fieldmap-dim)" }}>
                  {t("cancelButton")}
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="rounded px-4 py-2 text-sm disabled:opacity-50"
                  style={{ backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }}
                >
                  {submitting ? t("submitting") : t("submitButton")}
                </button>
              </div>
            </form>
          </>
        )}
      </div>
    </div>
  );
}
