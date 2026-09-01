"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { useAuthStore } from "@/store/useAuthStore";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { useCheckInsHistoryStore } from "@/store/useCheckInsHistoryStore";
import { deleteAccount, resendEmailVerification } from "@/lib/authApi";
import { getErrorMessage } from "@/lib/apiClient";

export default function SettingsPage() {
  const t = useTranslations("Settings");
  const tAuth = useTranslations("Auth");
  const router = useRouter();
  const user = useAuthStore((state) => state.user);
  const clearSession = useAuthStore((state) => state.clearSession);
  const resetNotifications = useNotificationsStore((state) => state.reset);
  const resetCheckInsHistory = useCheckInsHistoryStore((state) => state.reset);

  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [resending, setResending] = useState(false);
  const [resendError, setResendError] = useState<string | null>(null);
  const [resendSent, setResendSent] = useState(false);

  async function handleResendVerification() {
    setResendError(null);
    setResending(true);
    try {
      await resendEmailVerification();
      setResendSent(true);
    } catch (err) {
      setResendError(getErrorMessage(err, tAuth("unknownError")));
    } finally {
      setResending(false);
    }
  }

  async function handleDeleteAccount() {
    if (!window.confirm(t("deleteAccountConfirm"))) {
      return;
    }

    // Leave blank for external-auth-only accounts (Google/Facebook) — the backend only checks
    // it for accounts that have a password set.
    const password = window.prompt(t("deleteAccountPasswordPrompt"));
    if (password === null) {
      return;
    }

    setError(null);
    setDeleting(true);
    try {
      await deleteAccount(password || null);
      clearSession();
      resetNotifications();
      resetCheckInsHistory();
      router.push("/");
    } catch (err) {
      setError(getErrorMessage(err, t("deleteAccountError")));
      setDeleting(false);
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-xl flex-1 flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">{t("title")}</h1>

      {user && !user.isEmailVerified ? (
        <div
          className="flex flex-col gap-2 rounded-2xl border border-amber-500/40 p-5"
          style={{ backgroundColor: "var(--fieldmap-card)" }}
        >
          <p className="text-sm">{tAuth("emailNotVerifiedBanner")}</p>
          {resendSent ? (
            <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
              {tAuth("resendVerificationSuccess")}
            </p>
          ) : (
            <>
              {resendError ? <p className="text-sm text-red-700">{resendError}</p> : null}
              <button
                onClick={handleResendVerification}
                disabled={resending}
                className="w-fit rounded border px-4 py-2 text-sm disabled:opacity-50"
                style={{ borderColor: "var(--fieldmap-contour)" }}
              >
                {resending ? tAuth("resendVerificationSending") : tAuth("resendVerificationButton")}
              </button>
            </>
          )}
        </div>
      ) : null}

      <div className="flex flex-col gap-2 rounded-2xl p-5" style={{ backgroundColor: "var(--fieldmap-card)" }}>
        <h2 className="text-sm font-semibold uppercase" style={{ color: "var(--fieldmap-dim)" }}>
          {t("languageSectionTitle")}
        </h2>
        <LocaleSwitcher />
      </div>

      <div className="flex flex-col gap-2 rounded-2xl border border-red-700/40 p-5" style={{ backgroundColor: "var(--fieldmap-card)" }}>
        <h2 className="text-sm font-semibold uppercase text-red-700">{t("dangerZoneSectionTitle")}</h2>
        {error ? <p className="text-sm text-red-700">{error}</p> : null}
        <button
          onClick={handleDeleteAccount}
          disabled={deleting}
          className="w-fit rounded-full bg-red-700 px-4 py-2 text-sm font-medium text-white hover:bg-red-600 disabled:opacity-50"
        >
          {t("deleteAccountButton")}
        </button>
      </div>
    </div>
  );
}
