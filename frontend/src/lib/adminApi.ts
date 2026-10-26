import { apiFetch, apiFetchVoid } from "@/lib/apiClient";
import type { ReportReason } from "@/lib/reportsApi";
import type { SpotCategory, SpotChangeRequestType, SpotModerationStatus, SpotSearchResult } from "@/lib/spotsApi";
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

export interface AdminSpot extends SpotSearchResult {
  createdByDisplayName: string;
}

export interface AdminSpotsPageResponse {
  items: AdminSpot[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export type AdminSpotCounts = Record<"pending" | "approved" | "rejected", number>;

export function getAdminSpots(
  page: number,
  pageSize: number,
  filters: { status?: SpotModerationStatus; userId?: string; q?: string },
  signal?: AbortSignal,
): Promise<AdminSpotsPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (filters.status) params.set("status", filters.status);
  if (filters.userId) params.set("userId", filters.userId);
  if (filters.q) params.set("q", filters.q);
  return apiFetch<AdminSpotsPageResponse>(`/admin/spots?${params.toString()}`, { signal });
}

export function getAdminSpotCounts(): Promise<AdminSpotCounts> {
  return apiFetch<AdminSpotCounts>("/admin/spots/counts");
}

// Mirrors RejectionReasonRules.MaxLength on the backend — the limit for every rejection reason
// an admin writes (spot rejections and change request rejections).
export const MAX_REJECTION_REASON_LENGTH = 500;

export function approveSpot(id: string): Promise<void> {
  return apiFetchVoid(`/admin/spots/${id}/approve`, { method: "POST" });
}

export function rejectSpot(id: string, reason: string): Promise<void> {
  return apiFetchVoid(`/admin/spots/${id}/reject`, { method: "POST", body: JSON.stringify({ reason }) });
}

export interface AdminSpotChangeRequest {
  id: string;
  type: SpotChangeRequestType;
  createdAt: string;
  requestedByUserId: string;
  requestedByDisplayName: string;
  spotId: string;
  currentName: string;
  currentDescription: string;
  currentCategory: SpotCategory;
  currentPhotoUrls: string[];
  proposedName: string | null;
  proposedDescription: string | null;
  proposedCategory: SpotCategory | null;
  proposedPhotoUrls: string[] | null;
}

export interface AdminSpotChangeRequestsPageResponse {
  items: AdminSpotChangeRequest[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function getAdminSpotChangeRequests(page: number, pageSize: number): Promise<AdminSpotChangeRequestsPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<AdminSpotChangeRequestsPageResponse>(`/admin/spot-change-requests?${params.toString()}`);
}

export function approveSpotChangeRequest(id: string): Promise<void> {
  return apiFetchVoid(`/admin/spot-change-requests/${id}/approve`, { method: "POST" });
}

export function rejectSpotChangeRequest(id: string, reason: string): Promise<void> {
  return apiFetchVoid(`/admin/spot-change-requests/${id}/reject`, { method: "POST", body: JSON.stringify({ reason }) });
}
