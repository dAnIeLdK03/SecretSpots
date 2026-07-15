import { apiFetch } from "@/lib/apiClient";

export type NotificationType = "CrystalsEarned" | "NewSpotNearby";

export interface NotificationResponse {
  id: string;
  type: NotificationType;
  message: string;
  relatedSpotId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationsPageResponse {
  items: NotificationResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function fetchNotifications(page: number, pageSize: number): Promise<NotificationsPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<NotificationsPageResponse>(`/notifications?${params.toString()}`);
}

export function markNotificationRead(id: string): Promise<NotificationResponse> {
  return apiFetch<NotificationResponse>(`/notifications/${id}/read`, { method: "POST" });
}
