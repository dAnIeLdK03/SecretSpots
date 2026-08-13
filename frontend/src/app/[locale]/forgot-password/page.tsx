"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { requestPasswordReset } from "@/lib/authApi";
import { getErrorMessage } from "@/lib/apiClient";
import { AuthSplitLayout } from "@/components/AuthSplitLayout";

export default function ForgotPasswordPage() {
  const t = useTranslations("Auth");
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await requestPasswordReset(email);
      setSubmitted(true);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AuthSplitLayout title={t("forgotPasswordTitle")} subtitle={t("forgotPasswordSubtitle")}>
      <h2 className="text-2xl font-semibold">{t("forgotPasswordTitle")}</h2>

      {submitted ? (
        <p className="max-w-sm text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("forgotPasswordSuccess")}
        </p>
      ) : (
        <form onSubmit={handleSubmit} className="flex w-full max-w-sm flex-col gap-4">
          <label className="flex flex-col gap-1">
            <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
              {t("emailLabel")}
            </span>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="rounded border px-3 py-2"
              style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
            />
          </label>
          {error ? <p className="text-sm text-red-700">{error}</p> : null}
          <button
            type="submit"
            disabled={submitting}
            className="rounded px-4 py-2 disabled:opacity-50"
            style={{ backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }}
          >
            {submitting ? t("forgotPasswordSubmitting") : t("forgotPasswordButton")}
          </button>
        </form>
      )}

      <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        <Link href="/login" className="underline">
          {t("backToLogin")}
        </Link>
      </p>
    </AuthSplitLayout>
  );
}
