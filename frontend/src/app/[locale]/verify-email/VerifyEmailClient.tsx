"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { verifyEmail, getCurrentUser } from "@/lib/authApi";
import { getErrorMessage } from "@/lib/apiClient";
import { useAuthStore } from "@/store/useAuthStore";
import { AuthSplitLayout } from "@/components/AuthSplitLayout";

type VerifyState = "verifying" | "success" | "error";

export function VerifyEmailClient() {
  const t = useTranslations("Auth");
  const searchParams = useSearchParams();
  const token = searchParams.get("token");
  const accessToken = useAuthStore((state) => state.accessToken);
  const authStatus = useAuthStore((state) => state.status);
  const setSession = useAuthStore((state) => state.setSession);

  const [state, setState] = useState<VerifyState>(token ? "verifying" : "error");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;

    verifyEmail(token)
      .then(async () => {
        setState("success");
        // Refresh the cached profile so an already-logged-in session drops its "unverified"
        // banner immediately, without needing a fresh login.
        if (authStatus === "authenticated" && accessToken) {
          const user = await getCurrentUser();
          setSession(accessToken, user);
        }
      })
      .catch((err) => {
        setError(getErrorMessage(err, t("unknownError")));
        setState("error");
      });
    // Intentionally runs once on mount for the token in the URL — re-running on accessToken
    // changes would re-verify (and fail, since tokens are single-use) every time setSession fires.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  return (
    <AuthSplitLayout title={t("verifyEmailTitle")} subtitle={t("verifyEmailSubtitle")}>
      <h2 className="text-2xl font-semibold">{t("verifyEmailTitle")}</h2>

      {state === "verifying" ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("verifyEmailVerifying")}
        </p>
      ) : state === "success" ? (
        <p className="max-w-sm text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("verifyEmailSuccess")}
        </p>
      ) : (
        <p className="text-sm text-red-700 dark:text-red-400">{error ?? t("unknownError")}</p>
      )}

      <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        <Link href={authStatus === "authenticated" ? "/account" : "/login"} className="underline">
          {authStatus === "authenticated" ? t("backToAccount") : t("backToLogin")}
        </Link>
      </p>
    </AuthSplitLayout>
  );
}
