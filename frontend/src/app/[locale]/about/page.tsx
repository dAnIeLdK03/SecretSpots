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
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("intro")}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        {steps.map(({ icon: Icon, title, description }) => (
          <div
            key={title}
            className="flex flex-col items-center gap-2 rounded-2xl p-5 text-center shadow-sm"
            style={{ backgroundColor: "var(--fieldmap-card)" }}
          >
            <Icon size={28} style={{ color: "var(--fieldmap-trail)" }} />
            <h2 className="font-semibold">{title}</h2>
            <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
              {description}
            </p>
          </div>
        ))}
      </div>

      <div
        className="flex flex-col gap-2 rounded-2xl p-5"
        style={{ backgroundColor: "var(--fieldmap-card)" }}
      >
        <h2 className="text-lg font-semibold">{t("missionTitle")}</h2>
        <p className="text-sm" style={{ color: "var(--fieldmap-ink)" }}>
          {t("missionBody")}
        </p>
      </div>

      <Link
        href="/map"
        className="self-center rounded px-6 py-3 text-sm font-medium"
        style={{ backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }}
      >
        {t("ctaButton")}
      </Link>
    </div>
  );
}
