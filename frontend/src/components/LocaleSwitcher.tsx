"use client";

import { useLocale, useTranslations } from "next-intl";
import { usePathname, useRouter } from "@/i18n/navigation";
import { routing } from "@/i18n/routing";

const LOCALE_LABELS: Record<(typeof routing.locales)[number], string> = {
  bg: "БГ",
  en: "EN",
};

export function LocaleSwitcher() {
  const t = useTranslations("Layout");
  const activeLocale = useLocale();
  const pathname = usePathname();
  const router = useRouter();

  return (
    <div role="group" aria-label={t("languageLabel")} className="flex items-center gap-1 text-sm">
      {routing.locales.map((locale, index) => (
        <div key={locale} className="flex items-center gap-1">
          {index > 0 ? <span style={{ color: "var(--fieldmap-contour)" }}>|</span> : null}
          <button
            onClick={() => router.replace(pathname, { locale })}
            disabled={locale === activeLocale}
            aria-current={locale === activeLocale ? "true" : undefined}
            className={locale === activeLocale ? "font-semibold" : "underline opacity-60 hover:opacity-100"}
            style={{ color: "var(--fieldmap-ink)" }}
          >
            {LOCALE_LABELS[locale]}
          </button>
        </div>
      ))}
    </div>
  );
}
