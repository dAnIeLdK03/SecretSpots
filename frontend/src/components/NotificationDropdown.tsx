"use client";

import { useTranslations } from "next-intl";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { NotificationItem } from "@/components/NotificationItem";

export function NotificationDropdown({ onNavigate }: { onNavigate: () => void }) {
  const t = useTranslations("Notifications");
  const items = useNotificationsStore((state) => state.items);
  const status = useNotificationsStore((state) => state.status);
  const totalCount = useNotificationsStore((state) => state.totalCount);
  const loadMore = useNotificationsStore((state) => state.loadMore);

  const hasMore = items.length < totalCount;

  return (
    <div
      className="absolute right-0 top-full z-10 mt-2 max-h-96 w-80 overflow-y-auto rounded-md border shadow-lg"
      style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
    >
      <div className="border-b px-4 py-2 text-sm font-semibold" style={{ borderColor: "var(--fieldmap-contour)" }}>
        {t("title")}
      </div>

      {status === "loading" ? (
        <p className="px-4 py-6 text-center text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loading")}
        </p>
      ) : items.length === 0 ? (
        <p className="px-4 py-6 text-center text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("empty")}
        </p>
      ) : (
        <ul className="divide-y divide-[var(--fieldmap-contour)]">
          {items.map((notification) => (
            <li key={notification.id}>
              <NotificationItem notification={notification} onNavigate={onNavigate} />
            </li>
          ))}
        </ul>
      )}

      {hasMore && (
        <button
          onClick={() => loadMore()}
          disabled={status === "loadingMore"}
          className="w-full border-t px-4 py-2 text-center text-sm disabled:opacity-50"
          style={{ borderColor: "var(--fieldmap-contour)", color: "var(--fieldmap-dim)" }}
        >
          {status === "loadingMore" ? t("loadingMore") : t("loadMore")}
        </button>
      )}
    </div>
  );
}
