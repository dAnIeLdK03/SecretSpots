"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations, useLocale } from "next-intl";
import { formatRelativeTime } from "@/lib/relativeTime";
import { getErrorMessage } from "@/lib/apiClient";
import { useAuthStore } from "@/store/useAuthStore";
import { useSpotCommentsStore } from "@/store/useSpotCommentsStore";
import type { CommentResponse } from "@/lib/commentsApi";

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

  const isAuthor = user?.id === comment.userId;

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
      <li className="rounded p-3 text-sm" style={{ backgroundColor: "var(--fieldmap-paper-light)" }}>
        <form onSubmit={handleSaveEdit} className="flex flex-col gap-2">
          <textarea
            value={text}
            onChange={(e) => setText(e.target.value)}
            rows={2}
            className="w-full rounded border p-2 text-sm"
            style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
          />
          {error ? <p className="text-sm text-red-700">{error}</p> : null}
          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={() => {
                setEditing(false);
                setText(comment.text);
                setError(null);
              }}
              style={{ color: "var(--fieldmap-dim)" }}
            >
              {t("cancelButton")}
            </button>
            <button
              type="submit"
              disabled={submitting || !text.trim()}
              className="rounded px-3 py-1 disabled:opacity-50"
              style={{ backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }}
            >
              {submitting ? t("saving") : t("saveButton")}
            </button>
          </div>
        </form>
      </li>
    );
  }

  return (
    <li
      className="flex items-start justify-between gap-3 rounded p-3 text-sm"
      style={{ backgroundColor: "var(--fieldmap-paper-light)" }}
    >
      <div className="flex flex-col gap-1">
        <span className="font-medium" style={{ color: "var(--fieldmap-trail)" }}>
          {comment.authorDisplayName}
        </span>
        <p style={{ color: "var(--fieldmap-ink)" }}>{comment.text}</p>
        <span className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
          {formatRelativeTime(comment.createdAt, locale)}
          {comment.updatedAt ? ` · ${t("editedLabel")}` : ""}
        </span>
        {error ? <p className="text-sm text-red-700">{error}</p> : null}
      </div>
      {isAuthor ? (
        <div className="flex shrink-0 gap-2 text-xs">
          <button onClick={() => setEditing(true)} style={{ color: "var(--fieldmap-dim)" }}>
            {t("editButton")}
          </button>
          <button onClick={handleDelete} disabled={deleting} className="text-red-700 disabled:opacity-50">
            {t("deleteButton")}
          </button>
        </div>
      ) : null}
    </li>
  );
}
