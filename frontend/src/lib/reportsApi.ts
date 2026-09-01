import { apiFetch } from "@/lib/apiClient";

export const REPORT_REASONS = ["Spam", "Inappropriate", "Fake", "Other"] as const;
export type ReportReason = (typeof REPORT_REASONS)[number];

export interface ReportResponse {
  id: string;
}

export function reportSpot(spotId: string, reason: ReportReason, details?: string): Promise<ReportResponse> {
  return apiFetch<ReportResponse>(`/spots/${spotId}/reports`, {
    method: "POST",
    body: JSON.stringify({ reason, details: details || null }),
  });
}

export function reportComment(commentId: string, reason: ReportReason, details?: string): Promise<ReportResponse> {
  return apiFetch<ReportResponse>(`/comments/${commentId}/reports`, {
    method: "POST",
    body: JSON.stringify({ reason, details: details || null }),
  });
}
