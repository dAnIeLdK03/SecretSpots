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
          {index > 0 ? <span className="text-zinc-300 dark:text-zinc-700">|</span> : null}
          <button
            onClick={() => router.replace(pathname, { locale })}
            disabled={locale === activeLocale}
            aria-current={locale === activeLocale ? "true" : undefined}
            className={
              locale === activeLocale
                ? "font-semibold text-zinc-900 dark:text-zinc-100"
                : "text-zinc-500 underline hover:text-zinc-700 dark:text-zinc-500 dark:hover:text-zinc-300"
            }
          >
            {LOCALE_LABELS[locale]}
          </button>
        </div>
      ))}
    </div>
  );
}
