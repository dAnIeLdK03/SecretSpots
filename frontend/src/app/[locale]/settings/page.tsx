"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { useAuthStore } from "@/store/useAuthStore";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { useCheckInsHistoryStore } from "@/store/useCheckInsHistoryStore";
import { deleteAccount, resendEmailVerification } from "@/lib/authApi";
import { getErrorMessage } from "@/lib/apiClient";
import { ErrorMessage } from "@/components/ErrorMessage";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { useToast } from "@/hooks/useToast";

export default function SettingsPage() {
  const t = useTranslations("Settings");
  const tAuth = useTranslations("Auth");
  const showToast = useToast();
  const router = useRouter();
  const isAuthenticated = useRequireAuth();
  const user = useAuthStore((state) => state.user);
  const clearSession = useAuthStore((state) => state.clearSession);
  const resetNotifications = useNotificationsStore((state) => state.reset);
  const resetCheckInsHistory = useCheckInsHistoryStore((state) => state.reset);

  const [deleting, setDeleting] = useState(false);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [deletePassword, setDeletePassword] = useState("");
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

  function closeDeleteDialog() {
    setConfirmingDelete(false);
    setDeletePassword("");
  }

  async function handleDeleteAccount() {
    // Blank for external-auth-only accounts (Google/Facebook) — the backend only checks it for
    // accounts that have a password set.
    const password = deletePassword;
    closeDeleteDialog();
    setError(null);
    setDeleting(true);
    try {
      await deleteAccount(password || null);
      clearSession();
      resetNotifications();
      resetCheckInsHistory();
      showToast(t("toastAccountDeleted"));
      router.push("/");
    } catch (err) {
      setError(getErrorMessage(err, t("deleteAccountError")));
      setDeleting(false);
    }
  }

  if (!isAuthenticated || !user) {
    return null;
  }

  return (
    <div className="mx-auto flex w-full max-w-xl flex-1 flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">{t("title")}</h1>

      {user && !user.isEmailVerified ? (
        <div className="flex flex-col gap-2 rounded-2xl border border-amber-500/40 bg-card p-5">
          <p className="text-sm">{tAuth("emailNotVerifiedBanner")}</p>
          {resendSent ? (
            <p className="text-sm text-dim">{tAuth("resendVerificationSuccess")}</p>
          ) : (
            <>
              {resendError ? <ErrorMessage>{resendError}</ErrorMessage> : null}
              <button
                onClick={handleResendVerification}
                disabled={resending}
                className="w-fit rounded border border-contour px-4 py-2 text-sm disabled:opacity-50"
              >
                {resending ? tAuth("resendVerificationSending") : tAuth("resendVerificationButton")}
              </button>
            </>
          )}
        </div>
      ) : null}

      <div className="flex flex-col gap-2 rounded-2xl bg-card p-5">
        <h2 className="text-sm font-semibold text-dim uppercase">{t("languageSectionTitle")}</h2>
        <LocaleSwitcher />
      </div>

      <div className="flex flex-col gap-2 rounded-2xl border border-red-700/40 bg-card p-5">
        <h2 className="text-sm font-semibold text-red-700 uppercase dark:text-red-400">
          {t("dangerZoneSectionTitle")}
        </h2>
        {error ? <ErrorMessage>{error}</ErrorMessage> : null}
        <button
          onClick={() => setConfirmingDelete(true)}
          disabled={deleting}
          className="w-fit rounded-full bg-red-700 px-4 py-2 text-sm font-medium text-white hover:bg-red-600 disabled:opacity-50"
        >
          {t("deleteAccountButton")}
        </button>
      </div>

      {confirmingDelete ? (
        <ConfirmDialog
          title={t("deleteAccountButton")}
          message={t("deleteAccountConfirm")}
          confirmLabel={t("deleteAccountButton")}
          onConfirm={handleDeleteAccount}
          onCancel={closeDeleteDialog}
        >
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-dim">{t("deleteAccountPasswordPrompt")}</span>
            <input
              type="password"
              autoComplete="current-password"
              value={deletePassword}
              onChange={(e) => setDeletePassword(e.target.value)}
              className="rounded border border-contour bg-paper px-3 py-2"
            />
          </label>
        </ConfirmDialog>
      ) : null}
    </div>
  );
}
