"use client";

import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { exchangeExternalAuthCode, establishSession } from "@/lib/authApi";
import { getErrorMessage } from "@/lib/apiClient";

const KNOWN_ERROR_CODES = ["cancelled", "invalid_state", "provider_error"] as const;

export function CallbackClient() {
  const t = useTranslations("Auth");
  const router = useRouter();
  const searchParams = useSearchParams();
  const errorCode = searchParams.get("error");
  const code = searchParams.get("code");

  const [exchangeError, setExchangeError] = useState<string | null>(null);
  const ranOnce = useRef(false);

  useEffect(() => {
    if (errorCode || !code || ranOnce.current) {
      return;
    }
    ranOnce.current = true;

    exchangeExternalAuthCode(code)
      .then(establishSession)
      .then(() => router.push("/account"))
      .catch((err) => setExchangeError(getErrorMessage(err, t("unknownError"))));
  }, [errorCode, code, router, t]);

  const providerErrorKey = errorCode
    ? ((KNOWN_ERROR_CODES as readonly string[]).includes(errorCode) ? errorCode : "provider_error")
    : null;

  const error = providerErrorKey
    ? t(`externalAuthErrors.${providerErrorKey}`)
    : !code
      ? t("unknownError")
      : exchangeError;

  if (error) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-4 p-8 text-center">
        <p className="text-sm text-red-700 dark:text-red-400">{error}</p>
        <button
          type="button"
          onClick={() => router.push("/login")}
          className="rounded px-4 py-2 text-sm underline"
        >
          {t("loginLink")}
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4 p-8 text-center">
      <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {t("completingSignIn")}
      </p>
    </div>
  );
}
