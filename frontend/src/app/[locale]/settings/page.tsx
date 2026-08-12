"use client";

import { useTranslations } from "next-intl";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";

export default function SettingsPage() {
  const t = useTranslations("Settings");

  return (
    <div className="mx-auto flex w-full max-w-xl flex-1 flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">{t("title")}</h1>

      <div className="flex flex-col gap-2 rounded-2xl p-5" style={{ backgroundColor: "var(--fieldmap-card)" }}>
        <h2 className="text-sm font-semibold uppercase" style={{ color: "var(--fieldmap-dim)" }}>
          {t("languageSectionTitle")}
        </h2>
        <LocaleSwitcher />
      </div>
    </div>
  );
}
