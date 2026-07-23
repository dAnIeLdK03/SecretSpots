"use client";

import { useLocale } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { formatRelativeTime } from "@/lib/relativeTime";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import type { NotificationResponse, NotificationType } from "@/lib/notificationsApi";

const TYPE_ICONS: Record<NotificationType, string> = {
  CrystalsEarned: "💎",
  NewSpotNearby: "📍",
};

interface NotificationItemProps {
  notification: NotificationResponse;
  onNavigate: () => void;
}

export function NotificationItem({ notification, onNavigate }: NotificationItemProps) {
  const locale = useLocale();
  const router = useRouter();
  const markAsRead = useNotificationsStore((state) => state.markAsRead);

  function handleClick() {
    markAsRead(notification.id);
    if (notification.relatedSpotId) {
      router.push(`/spots/${notification.relatedSpotId}`);
      onNavigate();
    }
  }

  return (
    <button
      onClick={handleClick}
      className={`flex w-full items-start gap-3 px-4 py-3 text-left text-sm ${
        notification.isRead
          ? "text-zinc-600 dark:text-zinc-400"
          : "bg-blue-50 font-medium dark:bg-blue-950/40"
      }`}
    >
      <span aria-hidden="true">{TYPE_ICONS[notification.type]}</span>
      <span className="flex-1">
        <span className="block">{notification.message}</span>
        <span className="mt-1 block text-xs font-normal text-zinc-500">
          {formatRelativeTime(notification.createdAt, locale)}
        </span>
      </span>
      {!notification.isRead && (
        <span aria-hidden="true" className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-blue-600 dark:bg-blue-400" />
      )}
    </button>
  );
}
