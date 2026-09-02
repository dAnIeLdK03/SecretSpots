"use client";

import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/store/useAuthStore";
import { useSpotCommentsStore } from "@/store/useSpotCommentsStore";
import { getErrorMessage } from "@/lib/apiClient";
import { CommentListItem } from "@/components/CommentListItem";
import { Link } from "@/i18n/navigation";

export function CommentsSection({ spotId }: { spotId: string }) {
  const t = useTranslations("Comments");
  const tAuth = useTranslations("Auth");
  const authStatus = useAuthStore((state) => state.status);
  const items = useSpotCommentsStore((state) => state.items);
  const totalCount = useSpotCommentsStore((state) => state.totalCount);
  const status = useSpotCommentsStore((state) => state.status);
  const loadFirstPage = useSpotCommentsStore((state) => state.loadFirstPage);
  const loadMore = useSpotCommentsStore((state) => state.loadMore);
  const addComment = useSpotCommentsStore((state) => state.addComment);
  const reset = useSpotCommentsStore((state) => state.reset);

  const [text, setText] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadFirstPage(spotId);
    return () => reset();
  }, [spotId, loadFirstPage, reset]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await addComment(spotId, text);
      setText("");
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex flex-col gap-3 rounded-lg p-4" style={{ backgroundColor: "var(--fieldmap-card)" }}>
      <h2
        className="border-b pb-2 text-lg font-semibold"
        style={{ borderColor: "var(--fieldmap-contour)" }}
      >
        {t("title")}
      </h2>

      {authStatus === "authenticated" ? (
        <form onSubmit={handleSubmit} className="flex flex-col gap-2">
          <textarea
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder={t("placeholder")}
            rows={2}
            className="w-full rounded border p-2 text-sm"
            style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
          />
          {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}
          <div className="flex justify-end">
            <button
              type="submit"
              disabled={submitting || !text.trim()}
              className="rounded px-4 py-2 text-sm disabled:opacity-50"
              style={{ backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }}
            >
              {submitting ? t("submitting") : t("submitButton")}
            </button>
          </div>
        </form>
      ) : (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loginRequiredToComment")}{" "}
          <Link href="/login" className="underline">
            {tAuth("loginTitle")}
          </Link>
        </p>
      )}

      {status === "loading" ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loading")}
        </p>
      ) : items.length === 0 ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("empty")}
        </p>
      ) : (
        <ul className="flex flex-col gap-2">
          {items.map((comment) => (
            <CommentListItem key={comment.id} comment={comment} />
          ))}
        </ul>
      )}

      {items.length < totalCount ? (
        <button
          onClick={() => loadMore(spotId)}
          disabled={status === "loadingMore"}
          className="self-center rounded border px-4 py-2 text-sm disabled:opacity-50"
          style={{ borderColor: "var(--fieldmap-contour)" }}
        >
          {status === "loadingMore" ? t("loadingMore") : t("loadMore")}
        </button>
      ) : null}
    </div>
  );
}
