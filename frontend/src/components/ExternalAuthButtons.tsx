import { useTranslations } from "next-intl";
import { API_BASE_URL } from "@/lib/apiClient";

export function ExternalAuthButtons() {
  const t = useTranslations("Auth");

  return (
    <div className="flex w-full max-w-sm flex-col gap-2">
      <a
        href={`${API_BASE_URL}/auth/google`}
        className="rounded border px-4 py-2 text-center text-sm"
        style={{ borderColor: "var(--fieldmap-contour)", color: "var(--fieldmap-ink)" }}
      >
        {t("continueWithGoogle")}
      </a>
      <a
        href={`${API_BASE_URL}/auth/facebook`}
        className="rounded border px-4 py-2 text-center text-sm"
        style={{ borderColor: "var(--fieldmap-contour)", color: "var(--fieldmap-ink)" }}
      >
        {t("continueWithFacebook")}
      </a>
    </div>
  );
}
