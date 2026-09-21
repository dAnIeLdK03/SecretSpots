import { useTranslations } from "next-intl";

export default function Loading() {
  const t = useTranslations("Errors");

  return (
    <div className="flex flex-1 items-center justify-center p-8">
      <p role="status" className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {t("loading")}
      </p>
    </div>
  );
}
