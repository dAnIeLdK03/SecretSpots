"use client";

import { useLocale } from "next-intl";
import { formatRelativeTime } from "@/lib/relativeTime";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import type { NotificationResponse, NotificationType } from "@/lib/notificationsApi";

const TYPE_ICONS: Record<NotificationType, string> = {
  CrystalsEarned: "💎",
  NewSpotNearby: "📍",
};

export function NotificationItem({ notification }: { notification: NotificationResponse }) {
  const locale = useLocale();
  const markAsRead = useNotificationsStore((state) => state.markAsRead);

  return (
    <button
      onClick={() => markAsRead(notification.id)}
      className={`flex w-full items-start gap-3 px-4 py-3 text-left text-sm ${
        notification.isRead
          ? "text-zinc-600 dark:text-zinc-400"
          : "bg-zinc-100 font-medium dark:bg-zinc-800"
      }`}
    >
      <span aria-hidden="true">{TYPE_ICONS[notification.type]}</span>
      <span className="flex-1">
        <span className="block">{notification.message}</span>
        <span className="mt-1 block text-xs font-normal text-zinc-500">
          {formatRelativeTime(notification.createdAt, locale)}
        </span>
      </span>
    </button>
  );
}
