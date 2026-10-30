"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { requestPasswordReset } from "@/lib/authApi";
import { getErrorMessage } from "@/lib/apiClient";
import { AuthSplitLayout } from "@/components/AuthSplitLayout";
import { ErrorMessage } from "@/components/ErrorMessage";

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
        <p className="max-w-sm text-sm text-dim">{t("forgotPasswordSuccess")}</p>
      ) : (
        <form onSubmit={handleSubmit} className="flex w-full max-w-sm flex-col gap-4">
          <label className="flex flex-col gap-1">
            <span className="text-sm text-dim">{t("emailLabel")}</span>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="rounded border border-contour bg-paper-light px-3 py-2"
            />
          </label>
          {error ? <ErrorMessage>{error}</ErrorMessage> : null}
          <button
            type="submit"
            disabled={submitting}
            className="rounded bg-trail px-4 py-2 text-on-trail disabled:opacity-50"
          >
            {submitting ? t("forgotPasswordSubmitting") : t("forgotPasswordButton")}
          </button>
        </form>
      )}

      <p className="text-sm text-dim">
        <Link href="/login" className="underline">
          {t("backToLogin")}
        </Link>
      </p>
    </AuthSplitLayout>
  );
}
