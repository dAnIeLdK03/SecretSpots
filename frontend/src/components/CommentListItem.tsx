"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations, useLocale } from "next-intl";
import { formatRelativeTime } from "@/lib/relativeTime";
import { getErrorMessage } from "@/lib/apiClient";
import { useAuthStore } from "@/store/useAuthStore";
import { useSpotCommentsStore } from "@/store/useSpotCommentsStore";
import type { CommentResponse } from "@/lib/commentsApi";
import { ReportModal } from "@/components/ReportModal";
import { reportComment } from "@/lib/reportsApi";
import type { ReportReason } from "@/lib/reportsApi";
import { trackEvent } from "@/lib/analytics";

export function CommentListItem({ comment }: { comment: CommentResponse }) {
  const t = useTranslations("Comments");
  const locale = useLocale();
  const user = useAuthStore((state) => state.user);
  const editComment = useSpotCommentsStore((state) => state.editComment);
  const removeComment = useSpotCommentsStore((state) => state.removeComment);

  const [editing, setEditing] = useState(false);
  const [text, setText] = useState(comment.text);
  const [submitting, setSubmitting] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showReportModal, setShowReportModal] = useState(false);

  const isAuthor = user?.id === comment.userId;

  async function handleReportSubmit(reason: ReportReason, details: string) {
    await reportComment(comment.id, reason, details);
    trackEvent("report_submitted", { contentType: "comment", commentId: comment.id, reason });
  }

  async function handleSaveEdit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await editComment(comment.id, text);
      setEditing(false);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDelete() {
    if (!window.confirm(t("deleteConfirm"))) return;

    setDeleting(true);
    setError(null);
    try {
      await removeComment(comment.id);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
      setDeleting(false);
    }
  }

  if (editing) {
    return (
      <li className="rounded bg-paper-light p-3 text-sm">
        <form onSubmit={handleSaveEdit} className="flex flex-col gap-2">
          <textarea
            value={text}
            onChange={(e) => setText(e.target.value)}
            rows={2}
            aria-label={t("editCommentLabel")}
            className="w-full rounded border border-contour bg-paper-light p-2 text-sm"
          />
          {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}
          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={() => {
                setEditing(false);
                setText(comment.text);
                setError(null);
              }}
              className="text-dim"
            >
              {t("cancelButton")}
            </button>
            <button
              type="submit"
              disabled={submitting || !text.trim()}
              className="rounded bg-trail px-3 py-1 text-on-trail disabled:opacity-50"
            >
              {submitting ? t("saving") : t("saveButton")}
            </button>
          </div>
        </form>
      </li>
    );
  }

  return (
    <li className="flex items-start justify-between gap-3 rounded bg-paper-light p-3 text-sm">
      <div className="flex flex-col gap-1">
        <span className="font-medium text-link">{comment.authorDisplayName}</span>
        <p className="text-ink">{comment.text}</p>
        <span className="text-xs text-dim">
          {formatRelativeTime(comment.createdAt, locale)}
          {comment.updatedAt ? ` · ${t("editedLabel")}` : ""}
        </span>
        {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}
      </div>
      {isAuthor ? (
        <div className="flex shrink-0 gap-2 text-xs">
          <button onClick={() => setEditing(true)} className="text-dim">
            {t("editButton")}
          </button>
          <button
            onClick={handleDelete}
            disabled={deleting}
            className="text-red-700 disabled:opacity-50 dark:text-red-400"
          >
            {t("deleteButton")}
          </button>
        </div>
      ) : (
        <div className="flex shrink-0 gap-2 text-xs">
          <button onClick={() => setShowReportModal(true)} className="text-dim">
            {t("reportButton")}
          </button>
        </div>
      )}
      {showReportModal ? <ReportModal onClose={() => setShowReportModal(false)} onSubmit={handleReportSubmit} /> : null}
    </li>
  );
}
