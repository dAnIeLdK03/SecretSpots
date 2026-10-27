"use client";

import { useCallback, useEffect, useRef, useState } from "react";

export interface PageOf<T> {
  items: T[];
  totalCount: number;
}

// page is 1-based; signal aborts the request when the list is reloaded or unmounted.
export type PageFetcher<T> = (page: number, signal: AbortSignal) => Promise<PageOf<T>>;

export type PagedListStatus = "loading" | "idle" | "loadingMore";

export interface PagedList<T> {
  items: T[];
  totalCount: number;
  status: PagedListStatus;
  // Whatever the last failed load threw — callers turn it into text (getErrorMessage).
  error: unknown;
  hasMore: boolean;
  loadMore: () => void;
  reload: () => void;
  // For when an action moves an item out of this list (approved, dismissed, ...).
  removeItem: (id: string) => void;
}

// The "load page 1, then Load more" list every admin page and My spots used to hand-roll.
// Reloads from page 1 whenever `fetcher` changes identity — wrap it in useCallback with the
// filters it closes over as dependencies. A newer load aborts the older one, so a slow response
// for a previous filter can never overwrite the current list.
export function usePagedList<T extends { id: string }>(fetcher: PageFetcher<T>): PagedList<T> {
  const [items, setItems] = useState<T[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<PagedListStatus>("loading");
  const [error, setError] = useState<unknown>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const controllerRef = useRef<AbortController | null>(null);

  const load = useCallback(
    async (pageNum: number, append: boolean) => {
      controllerRef.current?.abort();
      const controller = new AbortController();
      controllerRef.current = controller;

      setStatus(append ? "loadingMore" : "loading");
      try {
        const result = await fetcher(pageNum, controller.signal);
        if (controller.signal.aborted) return;
        setItems((prev) => (append ? [...prev, ...result.items] : result.items));
        setTotalCount(result.totalCount);
        setPage(pageNum);
        setError(null);
      } catch (err) {
        if (controller.signal.aborted) return;
        setError(err);
      }
      setStatus("idle");
    },
    [fetcher],
  );

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- (re)loading page 1 when the fetcher/filters change, no user event to attach to
    void load(1, false);
    return () => controllerRef.current?.abort();
  }, [load, reloadKey]);

  const hasMore = items.length < totalCount;

  const loadMore = useCallback(() => {
    if (status !== "idle" || !hasMore) return;
    void load(page + 1, true);
  }, [status, hasMore, load, page]);

  const reload = useCallback(() => setReloadKey((key) => key + 1), []);

  // Callers only remove ids they got from `items`, so the count can be decremented
  // unconditionally (and outside the setItems updater, which StrictMode runs twice).
  const removeItem = useCallback((id: string) => {
    setItems((prev) => prev.filter((item) => item.id !== id));
    setTotalCount((count) => count - 1);
  }, []);

  return { items, totalCount, status, error, hasMore, loadMore, reload, removeItem };
}
