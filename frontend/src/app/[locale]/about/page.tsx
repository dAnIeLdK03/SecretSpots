"use client";

import { Compass, Camera, Gem } from "lucide-react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

export default function AboutPage() {
  const t = useTranslations("About");

  const steps = [
    { icon: Compass, title: t("step1Title"), description: t("step1Description") },
    { icon: Camera, title: t("step2Title"), description: t("step2Description") },
    { icon: Gem, title: t("step3Title"), description: t("step3Description") },
  ];

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-1 flex-col gap-8 p-8">
      <div className="flex flex-col gap-3 text-center">
        <h1 className="text-3xl font-semibold">{t("title")}</h1>
        <p className="text-sm text-dim">{t("intro")}</p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        {steps.map(({ icon: Icon, title, description }) => (
          <div key={title} className="flex flex-col items-center gap-2 rounded-2xl bg-card p-5 text-center shadow-sm">
            <Icon size={28} className="text-trail" />
            <h2 className="font-semibold">{title}</h2>
            <p className="text-sm text-dim">{description}</p>
          </div>
        ))}
      </div>

      <div className="flex flex-col gap-2 rounded-2xl bg-card p-5">
        <h2 className="text-lg font-semibold">{t("missionTitle")}</h2>
        <p className="text-sm text-ink">{t("missionBody")}</p>
      </div>

      <Link href="/map" className="self-center rounded bg-trail px-6 py-3 text-sm font-medium text-on-trail">
        {t("ctaButton")}
      </Link>
    </div>
  );
}
