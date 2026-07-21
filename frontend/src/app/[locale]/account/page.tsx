"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { useAuthStore } from "@/store/useAuthStore";
import { useCheckInsHistoryStore } from "@/store/useCheckInsHistoryStore";
import { CheckInHistoryItem } from "@/components/CheckInHistoryItem";

export default function AccountPage() {
  const t = useTranslations("Auth");
  const tHistory = useTranslations("CheckInsHistory");
  const isAuthenticated = useRequireAuth();
  const user = useAuthStore((state) => state.user);

  const items = useCheckInsHistoryStore((state) => state.items);
  const status = useCheckInsHistoryStore((state) => state.status);
  const totalCount = useCheckInsHistoryStore((state) => state.totalCount);
  const loadFirstPage = useCheckInsHistoryStore((state) => state.loadFirstPage);
  const loadMore = useCheckInsHistoryStore((state) => state.loadMore);

  useEffect(() => {
    if (isAuthenticated) {
      loadFirstPage();
    }
  }, [isAuthenticated, loadFirstPage]);

  if (!isAuthenticated || !user) {
    return null;
  }

  const hasMore = items.length < totalCount;

  return (
    <div className="flex flex-1 flex-col items-center gap-4 p-8">
      <h1 className="text-2xl font-semibold">{t("accountTitle")}</h1>
      <dl className="grid w-full max-w-sm grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
        <dt className="text-zinc-600 dark:text-zinc-400">{t("displayNameLabel")}</dt>
        <dd>{user.displayName}</dd>
        <dt className="text-zinc-600 dark:text-zinc-400">{t("emailLabel")}</dt>
        <dd>{user.email}</dd>
        <dt className="text-zinc-600 dark:text-zinc-400">{t("crystalBalanceLabel")}</dt>
        <dd>{user.crystalBalance}</dd>
      </dl>

      <div className="w-full max-w-sm">
        <h2 className="mb-2 text-sm font-semibold">{tHistory("title")}</h2>
        <div className="rounded-md border border-zinc-200 dark:border-zinc-800">
          {status === "loading" ? (
            <p className="px-4 py-6 text-center text-sm text-zinc-500">{tHistory("loading")}</p>
          ) : items.length === 0 ? (
            <p className="px-4 py-6 text-center text-sm text-zinc-500">{tHistory("empty")}</p>
          ) : (
            <ul className="divide-y divide-zinc-200 dark:divide-zinc-800">
              {items.map((checkIn) => (
                <CheckInHistoryItem key={checkIn.id} checkIn={checkIn} />
              ))}
            </ul>
          )}

          {hasMore && (
            <button
              onClick={() => loadMore()}
              disabled={status === "loadingMore"}
              className="w-full border-t border-zinc-200 px-4 py-2 text-center text-sm text-zinc-600 disabled:opacity-50 dark:border-zinc-800 dark:text-zinc-400"
            >
              {status === "loadingMore" ? tHistory("loadingMore") : tHistory("loadMore")}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
