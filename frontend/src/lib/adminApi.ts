import { apiFetch, apiFetchVoid, type PagedResponse } from "@/lib/apiClient";
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

export function getAdminReports(
  page: number,
  pageSize: number,
  includeResolved: boolean,
  signal?: AbortSignal,
): Promise<PagedResponse<AdminReport>> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
    includeResolved: String(includeResolved),
  });
  return apiFetch<PagedResponse<AdminReport>>(`/admin/reports?${params.toString()}`, { signal });
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

export function getAdminUsers(
  page: number,
  pageSize: number,
  q?: string,
  signal?: AbortSignal,
): Promise<PagedResponse<AdminUser>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (q) params.set("q", q);
  return apiFetch<PagedResponse<AdminUser>>(`/admin/users?${params.toString()}`, { signal });
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

export type AdminSpotCounts = Record<"pending" | "approved" | "rejected", number>;

export function getAdminSpots(
  page: number,
  pageSize: number,
  filters: { status?: SpotModerationStatus; userId?: string; q?: string },
  signal?: AbortSignal,
): Promise<PagedResponse<AdminSpot>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (filters.status) params.set("status", filters.status);
  if (filters.userId) params.set("userId", filters.userId);
  if (filters.q) params.set("q", filters.q);
  return apiFetch<PagedResponse<AdminSpot>>(`/admin/spots?${params.toString()}`, { signal });
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

export function getAdminSpotChangeRequests(
  page: number,
  pageSize: number,
  signal?: AbortSignal,
): Promise<PagedResponse<AdminSpotChangeRequest>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<PagedResponse<AdminSpotChangeRequest>>(`/admin/spot-change-requests?${params.toString()}`, {
    signal,
  });
}

export function approveSpotChangeRequest(id: string): Promise<void> {
  return apiFetchVoid(`/admin/spot-change-requests/${id}/approve`, { method: "POST" });
}

export function rejectSpotChangeRequest(id: string, reason: string): Promise<void> {
  return apiFetchVoid(`/admin/spot-change-requests/${id}/reject`, { method: "POST", body: JSON.stringify({ reason }) });
}

// A visit's proof photo at one spot; isOnSpot = already one of the spot's own photos.
export interface AdminSpotCheckIn {
  id: string;
  userId: string;
  userDisplayName: string;
  photoUrl: string;
  createdAt: string;
  isOnSpot: boolean;
}

export function getAdminSpotCheckIns(
  spotId: string,
  page: number,
  pageSize: number,
  signal?: AbortSignal,
): Promise<PagedResponse<AdminSpotCheckIn>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<PagedResponse<AdminSpotCheckIn>>(`/admin/spots/${spotId}/check-ins?${params.toString()}`, { signal });
}

export function addCheckInPhotoToSpot(checkInId: string): Promise<void> {
  return apiFetchVoid(`/admin/check-ins/${checkInId}/add-photo-to-spot`, { method: "POST" });
}
