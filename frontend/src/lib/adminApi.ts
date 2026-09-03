import { apiFetch, apiFetchVoid } from "@/lib/apiClient";
import type { ReportReason } from "@/lib/reportsApi";

export type ReportedContentType = "Spot" | "Comment";

export interface AdminReport {
  id: string;
  contentType: ReportedContentType;
  contentId: string;
  relatedSpotId: string | null;
  contentPreview: string | null;
  reporterDisplayName: string;
  reason: ReportReason;
  details: string | null;
  createdAt: string;
  resolvedAt: string | null;
}

export interface AdminReportsPageResponse {
  items: AdminReport[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function getAdminReports(
  page: number,
  pageSize: number,
  includeResolved: boolean,
): Promise<AdminReportsPageResponse> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
    includeResolved: String(includeResolved),
  });
  return apiFetch<AdminReportsPageResponse>(`/admin/reports?${params.toString()}`);
}

export function dismissReport(id: string): Promise<void> {
  return apiFetchVoid(`/admin/reports/${id}/dismiss`, { method: "POST" });
}

export function deleteReportedContent(id: string): Promise<void> {
  return apiFetchVoid(`/admin/reports/${id}/delete-content`, { method: "POST" });
}
