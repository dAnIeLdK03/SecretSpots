"use client";

import * as Sentry from "@sentry/nextjs";
import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

export default function RouteError({
  error,
  unstable_retry,
}: {
  error: Error & { digest?: string };
  unstable_retry: () => void;
}) {
  const t = useTranslations("Errors");

  useEffect(() => {
    Sentry.captureException(error);
  }, [error]);

  return (
    <div className="mx-auto flex w-full max-w-md flex-1 flex-col items-center justify-center gap-4 p-8 text-center">
      <h1 className="text-2xl font-semibold">{t("title")}</h1>
      <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {t("description")}
      </p>
      <div className="flex gap-3">
        <button
          onClick={() => unstable_retry()}
          className="rounded px-4 py-2 text-sm shadow"
          style={{ backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper-light)" }}
        >
          {t("retry")}
        </button>
        <Link
          href="/"
          className="rounded px-4 py-2 text-sm shadow"
          style={{ backgroundColor: "var(--fieldmap-paper-light)", color: "var(--fieldmap-ink)" }}
        >
          {t("backHome")}
        </Link>
      </div>
    </div>
  );
}
