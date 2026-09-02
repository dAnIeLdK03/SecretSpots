"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { resetPassword } from "@/lib/authApi";
import { getErrorMessage } from "@/lib/apiClient";
import { AuthSplitLayout } from "@/components/AuthSplitLayout";

export function ResetPasswordClient() {
  const t = useTranslations("Auth");
  const searchParams = useSearchParams();
  const token = searchParams.get("token");

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (newPassword !== confirmPassword) {
      setError(t("passwordsDoNotMatch"));
      return;
    }

    setSubmitting(true);
    try {
      await resetPassword(token ?? "", newPassword);
      setSubmitted(true);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AuthSplitLayout title={t("resetPasswordTitle")} subtitle={t("resetPasswordSubtitle")}>
      <h2 className="text-2xl font-semibold">{t("resetPasswordTitle")}</h2>

      {!token ? (
        <p className="text-sm text-red-700 dark:text-red-400">{t("unknownError")}</p>
      ) : submitted ? (
        <p className="max-w-sm text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("resetPasswordSuccess")}
        </p>
      ) : (
        <form onSubmit={handleSubmit} className="flex w-full max-w-sm flex-col gap-4">
          <label className="flex flex-col gap-1">
            <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
              {t("newPasswordLabel")}
            </span>
            <input
              type="password"
              required
              minLength={8}
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className="rounded border px-3 py-2"
              style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
            />
          </label>
          <label className="flex flex-col gap-1">
            <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
              {t("confirmPasswordLabel")}
            </span>
            <input
              type="password"
              required
              minLength={8}
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className="rounded border px-3 py-2"
              style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
            />
          </label>
          {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}
          <button
            type="submit"
            disabled={submitting}
            className="rounded px-4 py-2 disabled:opacity-50"
            style={{ backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }}
          >
            {submitting ? t("resetPasswordSubmitting") : t("resetPasswordButton")}
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
