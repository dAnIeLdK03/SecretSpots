import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

export default function NotFound() {
  const t = useTranslations("Errors");

  return (
    <div className="mx-auto flex w-full max-w-md flex-1 flex-col items-center justify-center gap-4 p-8 text-center">
      <h1 className="text-2xl font-semibold">{t("notFoundTitle")}</h1>
      <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {t("notFoundDescription")}
      </p>
      <Link
        href="/"
        className="rounded px-4 py-2 text-sm shadow"
        style={{ backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper-light)" }}
      >
        {t("backHome")}
      </Link>
    </div>
  );
}
