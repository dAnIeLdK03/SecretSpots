"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import * as Sentry from "@sentry/nextjs";
import { Link, useRouter } from "@/i18n/navigation";
import { login, establishSession, logout } from "@/lib/authApi";
import { useAuthStore } from "@/store/useAuthStore";
import { getErrorMessage } from "@/lib/apiClient";
import { AuthSplitLayout } from "@/components/AuthSplitLayout";
import { ExternalAuthButtons } from "@/components/ExternalAuthButtons";
import { useDraftState, clearDraftState } from "@/hooks/useDraftState";
import { trackEvent } from "@/lib/analytics";
import { ErrorMessage } from "@/components/ErrorMessage";

export default function LoginPage() {
  const t = useTranslations("Auth");
  const router = useRouter();
  const [email, setEmail] = useDraftState("login.email", "");
  const [password, setPassword] = useDraftState("login.password", "");
  const [rememberMe, setRememberMe] = useDraftState("login.rememberMe", false);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const restoredUser = useAuthStore((state) => (state.status === "authenticated" ? state.user : null));
  const [chooseAnother, setChooseAnother] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const result = await login(email, password, rememberMe);
      await establishSession(result);
      trackEvent("login");
      clearDraftState("login.email", "login.password", "login.rememberMe");
      router.push("/account");
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
      setSubmitting(false);
    }
  }

  async function handleUseAnother() {
    await logout().catch((err) => Sentry.captureException(err));
    useAuthStore.getState().clearSession();
    setChooseAnother(true);
  }

  return (
    <AuthSplitLayout title={t("loginWelcomeTitle")} subtitle={t("loginWelcomeSubtitle")}>
      <h2 className="text-2xl font-semibold">{t("loginTitle")}</h2>
      {restoredUser && !chooseAnother ? (
        <div className="flex w-full max-w-sm flex-col gap-3 rounded-md border border-contour p-4">
          <p className="text-sm">{t("continueAs", { name: restoredUser.displayName })}</p>
          <div className="flex gap-2">
            <Link href="/account" className="rounded bg-trail px-4 py-2 text-sm text-on-trail">
              {t("continueButton")}
            </Link>
            <button
              type="button"
              onClick={handleUseAnother}
              className="rounded border border-contour px-4 py-2 text-sm"
            >
              {t("useAnotherAccount")}
            </button>
          </div>
        </div>
      ) : null}
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
        <label className="flex flex-col gap-1">
          <span className="text-sm text-dim">{t("passwordLabel")}</span>
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="rounded border border-contour bg-paper-light px-3 py-2"
          />
        </label>
        <div className="flex items-center justify-between text-sm text-dim">
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={rememberMe} onChange={(e) => setRememberMe(e.target.checked)} />
            {t("rememberMeLabel")}
          </label>
          <Link href="/forgot-password" className="underline">
            {t("forgotPasswordLink")}
          </Link>
        </div>
        {error ? <ErrorMessage>{error}</ErrorMessage> : null}
        <button
          type="submit"
          disabled={submitting}
          className="rounded bg-trail px-4 py-2 text-on-trail disabled:opacity-50"
        >
          {submitting ? t("loginSubmitting") : t("loginButton")}
        </button>
      </form>
      <ExternalAuthButtons />
      <p className="text-sm text-dim">
        {t("noAccountYet")}{" "}
        <Link href="/register" className="underline">
          {t("registerLink")}
        </Link>
      </p>
    </AuthSplitLayout>
  );
}
