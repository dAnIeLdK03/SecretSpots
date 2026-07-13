"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { getHealth } from "@/lib/apiClient";

export default function Home() {
  const t = useTranslations("HomePage");
  const [status, setStatus] = useState<"loading" | "ok" | "error">("loading");
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    getHealth(controller.signal)
      .then((health) => {
        setStatus("ok");
        setMessage(health.status);
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        setStatus("error");
        setMessage(error instanceof Error ? error.message : t("unknownError"));
      });

    return () => controller.abort();
  }, [t]);

  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4 p-8">
      <h1 className="text-2xl font-semibold">{t("title")}</h1>
      <p className="text-zinc-600 dark:text-zinc-400">
        {t("backendStatus")}: <span className="font-mono">{status}</span>
        {message ? <span className="font-mono"> ({message})</span> : null}
      </p>
    </div>
  );
}
