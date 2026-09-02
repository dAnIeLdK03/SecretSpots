"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { register, establishSession } from "@/lib/authApi";
import { getErrorMessage } from "@/lib/apiClient";
import { AuthSplitLayout } from "@/components/AuthSplitLayout";
import { ExternalAuthButtons } from "@/components/ExternalAuthButtons";

export default function RegisterPage() {
  const t = useTranslations("Auth");
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const result = await register(email, password, displayName);
      await establishSession(result);
      router.push("/account");
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
      setSubmitting(false);
    }
  }

  return (
    <AuthSplitLayout title={t("registerWelcomeTitle")} subtitle={t("registerWelcomeSubtitle")}>
      <h2 className="text-2xl font-semibold">{t("registerTitle")}</h2>
      <form onSubmit={handleSubmit} className="flex w-full max-w-sm flex-col gap-4">
        <label className="flex flex-col gap-1">
          <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
            {t("displayNameLabel")}
          </span>
          <input
            type="text"
            required
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="rounded border px-3 py-2"
            style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
          />
        </label>
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
        <label className="flex flex-col gap-1">
          <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
            {t("passwordLabel")}
          </span>
          <input
            type="password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
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
          {submitting ? t("registerSubmitting") : t("registerButton")}
        </button>
      </form>
      <ExternalAuthButtons />
      <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {t("alreadyHaveAccount")}{" "}
        <Link href="/login" className="underline">
          {t("loginLink")}
        </Link>
      </p>
    </AuthSplitLayout>
  );
}
