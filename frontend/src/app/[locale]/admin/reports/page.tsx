"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { useAuthStore } from "@/store/useAuthStore";
import { useRouter, Link } from "@/i18n/navigation";
import { getErrorMessage } from "@/lib/apiClient";
import { getAdminReports, dismissReport, deleteReportedContent } from "@/lib/adminApi";
import type { AdminReport } from "@/lib/adminApi";
import { formatRelativeTime } from "@/lib/relativeTime";
import { useLocale } from "next-intl";

const PAGE_SIZE = 20;

export default function AdminReportsPage() {
  const t = useTranslations("AdminReports");
  const tReports = useTranslations("Reports");
  const locale = useLocale();
  const router = useRouter();
  const isAuthenticated = useRequireAuth();
  const user = useAuthStore((state) => state.user);

  const [includeResolved, setIncludeResolved] = useState(false);
  const [items, setItems] = useState<AdminReport[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<"loading" | "idle" | "loadingMore">("loading");
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  useEffect(() => {
    if (isAuthenticated && user && !user.isAdmin) {
      router.replace("/");
    }
  }, [isAuthenticated, user, router]);

  const fetchPage = useCallback(
    (pageNum: number, append: boolean) => {
      setStatus(append ? "loadingMore" : "loading");
      return getAdminReports(pageNum, PAGE_SIZE, includeResolved)
        .then((result) => {
          setItems((prev) => (append ? [...prev, ...result.items] : result.items));
          setTotalCount(result.totalCount);
          setPage(pageNum);
          setError(null);
        })
        .catch((err) => setError(getErrorMessage(err, t("unknownError"))))
        .finally(() => setStatus("idle"));
    },
    [includeResolved, t],
  );

  useEffect(() => {
    if (isAuthenticated && user?.isAdmin) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- kicking off the initial fetch on mount, no user event to attach to
      void fetchPage(1, false);
    }
  }, [isAuthenticated, user?.isAdmin, fetchPage]);

  async function handleDismiss(id: string) {
    setBusyId(id);
    try {
      await dismissReport(id);
      setItems((prev) => prev.filter((r) => r.id !== id));
      setTotalCount((prev) => prev - 1);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setBusyId(null);
    }
  }

  async function handleDeleteContent(report: AdminReport) {
    const typeLabel = report.contentType === "Spot" ? t("contentTypeSpot") : t("contentTypeComment");
    if (!window.confirm(t("deleteContentConfirm", { type: typeLabel }))) return;

    setBusyId(report.id);
    try {
      await deleteReportedContent(report.id);
      setItems((prev) => prev.filter((r) => r.id !== report.id));
      setTotalCount((prev) => prev - 1);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setBusyId(null);
    }
  }

  if (!isAuthenticated || !user?.isAdmin) {
    return null;
  }

  const hasMore = items.length < totalCount;

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-1 flex-col gap-4 p-8">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">{t("title")}</h1>
        <label className="flex items-center gap-2 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          <input
            type="checkbox"
            checked={includeResolved}
            onChange={(e) => setIncludeResolved(e.target.checked)}
          />
          {t("includeResolvedLabel")}
        </label>
      </div>

      {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}

      {status === "loading" ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loading")}
        </p>
      ) : items.length === 0 ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("empty")}
        </p>
      ) : (
        <ul className="flex flex-col gap-3">
          {items.map((report) => (
            <li
              key={report.id}
              className="flex flex-col gap-2 rounded-2xl p-4"
              style={{ backgroundColor: "var(--fieldmap-card)" }}
            >
              <div className="flex flex-wrap items-center justify-between gap-2 text-xs" style={{ color: "var(--fieldmap-dim)" }}>
                <span>
                  {report.contentType === "Spot" ? t("contentTypeSpot") : t("contentTypeComment")} ·{" "}
                  {tReports(`reason.${report.reason}`)} · {formatRelativeTime(report.createdAt, locale)}
                </span>
                {report.resolvedAt ? (
                  <span>
                    {report.resolutionAction && report.resolvedByDisplayName
                      ? t("resolvedSummary", { action: report.resolutionAction, name: report.resolvedByDisplayName })
                      : t("resolvedLabel")}
                  </span>
                ) : null}
              </div>

              <p className="text-sm" style={{ color: "var(--fieldmap-ink)" }}>
                {report.contentPreview ?? t("contentDeleted")}
              </p>

              {report.details ? (
                <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
                  {t("detailsLabel")}: {report.details}
                </p>
              ) : null}

              <p className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
                {t("reporterLabel")}: {report.reporterDisplayName}
              </p>

              <div className="flex flex-wrap items-center gap-3 text-sm">
                {report.relatedSpotId ? (
                  <Link href={`/spots/${report.relatedSpotId}`} className="underline">
                    {t("viewSpotLink")}
                  </Link>
                ) : null}

                {!report.resolvedAt && (
                  <>
                    <button
                      onClick={() => handleDismiss(report.id)}
                      disabled={busyId === report.id}
                      className="rounded border px-3 py-1 disabled:opacity-50"
                      style={{ borderColor: "var(--fieldmap-contour)" }}
                    >
                      {t("dismissButton")}
                    </button>
                    {report.contentPreview !== null ? (
                      <button
                        onClick={() => handleDeleteContent(report)}
                        disabled={busyId === report.id}
                        className="rounded border border-red-300 dark:border-red-800 px-3 py-1 text-red-700 dark:text-red-400 disabled:opacity-50"
                      >
                        {t("deleteContentButton")}
                      </button>
                    ) : null}
                  </>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      {hasMore ? (
        <button
          onClick={() => fetchPage(page + 1, true)}
          disabled={status === "loadingMore"}
          className="rounded border px-4 py-2 text-sm disabled:opacity-50"
          style={{ borderColor: "var(--fieldmap-contour)" }}
        >
          {status === "loadingMore" ? t("loadingMore") : t("loadMore")}
        </button>
      ) : null}
    </div>
  );
}
