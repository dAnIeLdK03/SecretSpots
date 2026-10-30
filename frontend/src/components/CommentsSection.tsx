"use client";

import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/store/useAuthStore";
import { useSpotCommentsStore } from "@/store/useSpotCommentsStore";
import { getErrorMessage } from "@/lib/apiClient";
import { CommentListItem } from "@/components/CommentListItem";
import { Link } from "@/i18n/navigation";
import { ErrorMessage } from "@/components/ErrorMessage";
import { StatusMessage } from "@/components/StatusMessage";

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
    <div className="flex flex-col gap-3 rounded-lg bg-card p-4">
      <h2 className="border-b border-contour pb-2 text-lg font-semibold">{t("title")}</h2>

      {authStatus === "authenticated" ? (
        <form onSubmit={handleSubmit} className="flex flex-col gap-2">
          <textarea
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder={t("placeholder")}
            aria-label={t("placeholder")}
            rows={2}
            className="w-full rounded border border-contour bg-paper-light p-2 text-sm"
          />
          {error ? <ErrorMessage>{error}</ErrorMessage> : null}
          <div className="flex justify-end">
            <button
              type="submit"
              disabled={submitting || !text.trim()}
              className="rounded bg-trail px-4 py-2 text-sm text-on-trail disabled:opacity-50"
            >
              {submitting ? t("submitting") : t("submitButton")}
            </button>
          </div>
        </form>
      ) : (
        <p className="text-sm text-dim">
          {t("loginRequiredToComment")}{" "}
          <Link href="/login" className="underline">
            {tAuth("loginTitle")}
          </Link>
        </p>
      )}

      {status === "loading" ? (
        <StatusMessage>{t("loading")}</StatusMessage>
      ) : items.length === 0 ? (
        <p className="text-sm text-dim">{t("empty")}</p>
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
          className="self-center rounded border border-contour px-4 py-2 text-sm disabled:opacity-50"
        >
          {status === "loadingMore" ? t("loadingMore") : t("loadMore")}
        </button>
      ) : null}
    </div>
  );
}
