import { apiFetch, apiFetchVoid } from "@/lib/apiClient";
import type { ReportReason } from "@/lib/reportsApi";
import type { SpotModerationStatus, SpotSearchPageResponse } from "@/lib/spotsApi";
import type { UserRole } from "@/lib/userRole";

export type ReportedContentType = "Spot" | "Comment";

export type ReportResolutionAction = "Dismissed" | "ContentDeleted" | "ContentDeletedByAuthor";

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
  resolvedByDisplayName: string | null;
  resolutionAction: ReportResolutionAction | null;
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

export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
}

export interface AdminUsersPageResponse {
  items: AdminUser[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function getAdminUsers(page: number, pageSize: number, q?: string): Promise<AdminUsersPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (q) params.set("q", q);
  return apiFetch<AdminUsersPageResponse>(`/admin/users?${params.toString()}`);
}

export type ExternalAuthProvider = "Google" | "Facebook";

export interface AdminUserActivity {
  spotsPending: number;
  spotsApproved: number;
  spotsRejected: number;
  comments: number;
  checkIns: number;
  ratings: number;
  reportsFiled: number;
  crystalBalance: number;
}

export interface AdminUserDetails {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  isEmailVerified: boolean;
  hasPassword: boolean;
  createdAt: string;
  linkedProviders: ExternalAuthProvider[];
  activity: AdminUserActivity;
}

export function getAdminUser(id: string, signal?: AbortSignal): Promise<AdminUserDetails> {
  return apiFetch<AdminUserDetails>(`/admin/users/${id}`, { signal });
}

export function getAdminSpots(
  page: number,
  pageSize: number,
  filters: { status?: SpotModerationStatus; userId?: string },
  signal?: AbortSignal,
): Promise<SpotSearchPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (filters.status) params.set("status", filters.status);
  if (filters.userId) params.set("userId", filters.userId);
  return apiFetch<SpotSearchPageResponse>(`/admin/spots?${params.toString()}`, { signal });
}

export function approveSpot(id: string): Promise<void> {
  return apiFetchVoid(`/admin/spots/${id}/approve`, { method: "POST" });
}

export function rejectSpot(id: string, reason: string): Promise<void> {
  return apiFetchVoid(`/admin/spots/${id}/reject`, { method: "POST", body: JSON.stringify({ reason }) });
}
