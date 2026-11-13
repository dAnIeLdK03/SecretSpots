import { apiFetch, apiFetchVoid, type PagedResponse } from "@/lib/apiClient";

export type NotificationType =
  | "NewSpotNearby"
  | "NewCommentOnYourSpot"
  | "NewRatingOnYourSpot"
  | "YourContentRemoved"
  | "ReportSubmitted"
  | "NewVisitorOnYourSpot"
  | "SpotSubmittedForReview"
  | "SpotChangeRequestSubmitted"
  | "SpotApproved"
  | "SpotRejected"
  | "SpotRemoved"
  | "SpotRestored"
  | "SpotEditApproved"
  | "SpotEditRejected"
  | "SpotDeletionApproved"
  | "SpotDeletionRejected"
  | "CheckInPhotoAddedToSpot"
  | "RejectedSpotDeleted";

export interface NotificationResponse {
  id: string;
  type: NotificationType;
  message: string;
  relatedSpotId: string | null;
  isRead: boolean;
  createdAt: string;
}

export function fetchNotifications(page: number, pageSize: number): Promise<PagedResponse<NotificationResponse>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<PagedResponse<NotificationResponse>>(`/notifications?${params.toString()}`);
}

export function markNotificationRead(id: string): Promise<NotificationResponse> {
  return apiFetch<NotificationResponse>(`/notifications/${id}/read`, { method: "POST" });
}

export function markAllNotificationsRead(): Promise<void> {
  return apiFetchVoid("/notifications/read-all", { method: "POST" });
}
