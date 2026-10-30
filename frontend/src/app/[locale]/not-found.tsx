import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

export default function NotFound() {
  const t = useTranslations("Errors");

  return (
    <div className="mx-auto flex w-full max-w-md flex-1 flex-col items-center justify-center gap-4 p-8 text-center">
      <h1 className="text-2xl font-semibold">{t("notFoundTitle")}</h1>
      <p className="text-sm text-dim">{t("notFoundDescription")}</p>
      <Link href="/" className="rounded bg-ink px-4 py-2 text-sm text-paper-light shadow">
        {t("backHome")}
      </Link>
    </div>
  );
}
