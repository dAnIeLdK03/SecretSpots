"use client";

import { forwardRef, useLayoutEffect, useState, type RefObject } from "react";
import { createPortal } from "react-dom";
import { useTranslations } from "next-intl";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { NotificationItem } from "@/components/NotificationItem";

interface NotificationDropdownProps {
  anchorRef: RefObject<HTMLElement | null>;
  onNavigate: () => void;
}

// Portaled to document.body (rather than positioned relative to the bell in-place) because
// MapLibre's WebGL canvas gets its own GPU compositing layer, which some browsers paint above
// regular DOM content regardless of CSS z-index — a plain in-place dropdown ends up rendered
// behind the map on the /map page even with a higher stack level. Escaping to body sidesteps
// that entirely.
export const NotificationDropdown = forwardRef<HTMLDivElement, NotificationDropdownProps>(
  function NotificationDropdown({ anchorRef, onNavigate }, ref) {
    const t = useTranslations("Notifications");
    const items = useNotificationsStore((state) => state.items);
    const status = useNotificationsStore((state) => state.status);
    const totalCount = useNotificationsStore((state) => state.totalCount);
    const loadMore = useNotificationsStore((state) => state.loadMore);
    const markAllAsRead = useNotificationsStore((state) => state.markAllAsRead);
    const hasUnread = items.some((n) => !n.isRead);

    const [position, setPosition] = useState<{ top: number; right: number } | null>(null);

    useLayoutEffect(() => {
      const anchor = anchorRef.current;
      if (!anchor) return;
      const rect = anchor.getBoundingClientRect();
      setPosition({ top: rect.bottom + 8, right: window.innerWidth - rect.right });
    }, [anchorRef]);

    const hasMore = items.length < totalCount;

    if (!position) return null;

    return createPortal(
      <div
        ref={ref}
        className="fixed z-50 max-h-96 w-80 overflow-y-auto rounded-md border shadow-lg"
        style={{
          top: position.top,
          right: position.right,
          borderColor: "var(--fieldmap-contour)",
          backgroundColor: "var(--fieldmap-paper-light)",
        }}
      >
        <div
          className="flex items-center justify-between border-b px-4 py-2"
          style={{ borderColor: "var(--fieldmap-contour)" }}
        >
          <span className="text-sm font-semibold">{t("title")}</span>
          {hasUnread ? (
            <button
              onClick={() => markAllAsRead()}
              className="text-xs underline"
              style={{ color: "var(--fieldmap-dim)" }}
            >
              {t("markAllAsRead")}
            </button>
          ) : null}
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
      </div>,
      document.body,
    );
  },
);
