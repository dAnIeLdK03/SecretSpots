"use client";

import { useLocale } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { formatRelativeTime } from "@/lib/relativeTime";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import type { NotificationResponse, NotificationType } from "@/lib/notificationsApi";

const TYPE_ICONS: Record<NotificationType, string> = {
  CrystalsEarned: "💎",
  NewSpotNearby: "📍",
  NewCommentOnYourSpot: "💬",
  NewRatingOnYourSpot: "⭐",
  ReportSubmitted: "🚩",
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
    if (notification.type === "ReportSubmitted") {
      router.push("/admin/reports");
      onNavigate();
    } else if (notification.relatedSpotId) {
      router.push(`/spots/${notification.relatedSpotId}`);
      onNavigate();
    }
  }

  return (
    <button
      onClick={handleClick}
      className={`flex w-full items-start gap-3 px-4 py-3 text-left text-sm ${
        notification.isRead ? "" : "bg-blue-50 dark:bg-blue-950 font-medium"
      }`}
      style={notification.isRead ? { color: "var(--fieldmap-dim)" } : undefined}
    >
      <span aria-hidden="true">{TYPE_ICONS[notification.type]}</span>
      <span className="flex-1">
        <span className="block">{notification.message}</span>
        <span className="mt-1 block text-xs font-normal" style={{ color: "var(--fieldmap-dim)" }}>
          {formatRelativeTime(notification.createdAt, locale)}
        </span>
      </span>
      {!notification.isRead && (
        <span aria-hidden="true" className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-blue-600" />
      )}
    </button>
  );
}
