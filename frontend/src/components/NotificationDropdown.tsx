"use client";

import { useTranslations } from "next-intl";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { NotificationItem } from "@/components/NotificationItem";

export function NotificationDropdown() {
  const t = useTranslations("Notifications");
  const items = useNotificationsStore((state) => state.items);
  const status = useNotificationsStore((state) => state.status);
  const totalCount = useNotificationsStore((state) => state.totalCount);
  const loadMore = useNotificationsStore((state) => state.loadMore);

  const hasMore = items.length < totalCount;

  return (
    <div className="absolute right-0 top-full z-10 mt-2 max-h-96 w-80 overflow-y-auto rounded-md border border-zinc-200 bg-white shadow-lg dark:border-zinc-800 dark:bg-zinc-900">
      <div className="border-b border-zinc-200 px-4 py-2 text-sm font-semibold dark:border-zinc-800">
        {t("title")}
      </div>

      {status === "loading" ? (
        <p className="px-4 py-6 text-center text-sm text-zinc-500">{t("loading")}</p>
      ) : items.length === 0 ? (
        <p className="px-4 py-6 text-center text-sm text-zinc-500">{t("empty")}</p>
      ) : (
        <ul className="divide-y divide-zinc-200 dark:divide-zinc-800">
          {items.map((notification) => (
            <li key={notification.id}>
              <NotificationItem notification={notification} />
            </li>
          ))}
        </ul>
      )}

      {hasMore && (
        <button
          onClick={() => loadMore()}
          disabled={status === "loadingMore"}
          className="w-full border-t border-zinc-200 px-4 py-2 text-center text-sm text-zinc-600 disabled:opacity-50 dark:border-zinc-800 dark:text-zinc-400"
        >
          {status === "loadingMore" ? t("loadingMore") : t("loadMore")}
        </button>
      )}
    </div>
  );
}
