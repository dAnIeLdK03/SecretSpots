"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { getErrorMessage } from "@/lib/apiClient";
import { getAdminReports, dismissReport, deleteReportedContent } from "@/lib/adminApi";
import type { AdminReport } from "@/lib/adminApi";
import { formatRelativeTime } from "@/lib/relativeTime";
import { usePagedList } from "@/hooks/usePagedList";
import { LoadMoreButton } from "@/components/LoadMoreButton";
import { useLocale } from "next-intl";

const PAGE_SIZE = 20;

export default function AdminReportsPage() {
  const t = useTranslations("AdminReports");
  const tReports = useTranslations("Reports");
  const locale = useLocale();

  const [includeResolved, setIncludeResolved] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const fetchReports = useCallback(
    (page: number, signal: AbortSignal) => getAdminReports(page, PAGE_SIZE, includeResolved, signal),
    [includeResolved],
  );
  const list = usePagedList(fetchReports);
  const { items, status } = list;
  const error = actionError ?? (list.error ? getErrorMessage(list.error, t("unknownError")) : null);

  async function handleDismiss(id: string) {
    setBusyId(id);
    try {
      await dismissReport(id);
      list.removeItem(id);
    } catch (err) {
      setActionError(getErrorMessage(err, t("unknownError")));
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
      list.removeItem(report.id);
    } catch (err) {
      setActionError(getErrorMessage(err, t("unknownError")));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-1 flex-col gap-4 p-8">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">{t("title")}</h1>
        <label className="flex items-center gap-2 text-sm text-dim">
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
        <p className="text-sm text-dim">{t("loading")}</p>
      ) : items.length === 0 ? (
        <p className="text-sm text-dim">{t("empty")}</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {items.map((report) => (
            <li
              key={report.id}
              className="flex flex-col gap-2 rounded-2xl p-4 bg-card"
            >
              <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-dim">
                <span>
                  {report.contentType === "Spot" ? t("contentTypeSpot") : t("contentTypeComment")} ·{" "}
                  {tReports(`reason.${report.reason}`)} · {formatRelativeTime(report.createdAt, locale)}
                </span>
                {report.resolvedAt ? (
                  <span>
                    {report.resolutionAction === "ContentDeletedByAuthor"
                      ? t("resolvedByAuthorLabel")
                      : report.resolutionAction && report.resolvedByDisplayName
                        ? t("resolvedSummary", { action: report.resolutionAction, name: report.resolvedByDisplayName })
                        : t("resolvedLabel")}
                  </span>
                ) : null}
              </div>

              <p className="text-sm text-ink">{report.contentPreview ?? t("contentDeleted")}</p>

              {report.details ? (
                <p className="text-sm text-dim">{t("detailsLabel")}: {report.details}</p>
              ) : null}

              <p className="text-xs text-dim">{t("reporterLabel")}: {report.reporterDisplayName}</p>

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
                      className="rounded border px-3 py-1 disabled:opacity-50 border-contour"
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

      <LoadMoreButton list={list} />
    </div>
  );
}
