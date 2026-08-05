import { useTranslations } from "next-intl";
import { API_BASE_URL } from "@/lib/apiClient";

function GoogleIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 48 48" aria-hidden="true">
      <path
        fill="#FFC107"
        d="M43.6 20.5H42V20H24v8h11.3c-1.6 4.7-6.1 8-11.3 8-6.6 0-12-5.4-12-12s5.4-12 12-12c3.1 0 5.9 1.2 8 3.1l5.7-5.7C34.5 6.1 29.5 4 24 4 12.9 4 4 12.9 4 24s8.9 20 20 20 20-8.9 20-20c0-1.3-.1-2.7-.4-3.5z"
      />
      <path
        fill="#FF3D00"
        d="M6.3 14.7l6.6 4.8C14.6 15.9 18.9 13 24 13c3.1 0 5.9 1.2 8 3.1l5.7-5.7C34.5 6.1 29.5 4 24 4c-7.7 0-14.4 4.4-17.7 10.7z"
      />
      <path
        fill="#4CAF50"
        d="M24 44c5.4 0 10.3-2.1 14-5.5l-6.5-5.5c-2 1.4-4.6 2.3-7.5 2.3-5.2 0-9.6-3.3-11.3-7.9l-6.5 5C9.5 39.6 16.2 44 24 44z"
      />
      <path
        fill="#1976D2"
        d="M43.6 20.5H42V20H24v8h11.3c-.8 2.3-2.2 4.2-4.1 5.6l6.5 5.5C41 36 44 30.5 44 24c0-1.3-.1-2.7-.4-3.5z"
      />
    </svg>
  );
}

function FacebookIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" aria-hidden="true">
      <path
        fill="#1877F2"
        d="M24 12.1c0-6.6-5.4-12-12-12S0 5.5 0 12.1C0 18 4.4 22.9 10.1 24v-8.4H7.1v-3.5h3v-2.6c0-3 1.8-4.6 4.5-4.6 1.3 0 2.6.2 2.6.2v2.9h-1.5c-1.5 0-1.9.9-1.9 1.9v2.2h3.3l-.5 3.5h-2.8V24C19.6 22.9 24 18 24 12.1z"
      />
    </svg>
  );
}

export function ExternalAuthButtons() {
  const t = useTranslations("Auth");

  return (
    <div className="flex w-full max-w-sm flex-col items-center gap-3">
      <div className="flex w-full items-center gap-3">
        <hr className="flex-1" style={{ borderColor: "var(--fieldmap-contour)" }} />
        <span className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
          {t("orDivider")}
        </span>
        <hr className="flex-1" style={{ borderColor: "var(--fieldmap-contour)" }} />
      </div>
      <div className="flex gap-3">
        <a
          href={`${API_BASE_URL}/auth/google`}
          aria-label={t("continueWithGoogle")}
          title={t("continueWithGoogle")}
          className="flex h-11 w-11 items-center justify-center rounded-full transition-colors hover:bg-black/5"
        >
          <GoogleIcon />
        </a>
        <a
          href={`${API_BASE_URL}/auth/facebook`}
          aria-label={t("continueWithFacebook")}
          title={t("continueWithFacebook")}
          className="flex h-11 w-11 items-center justify-center rounded-full transition-colors hover:bg-black/5"
        >
          <FacebookIcon />
        </a>
      </div>
    </div>
  );
}
