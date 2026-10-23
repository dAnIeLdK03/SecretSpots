import { apiFetch, apiFetchVoid } from "@/lib/apiClient";

export const SPOT_CATEGORIES = [
  "Nature",
  "Viewpoint",
  "Cafe",
  "Abandoned",
  "Waterfall",
  "Cave",
  "Landmark",
  "BeachOrLake",
  "Spring",
  "MonasteryOrChurch",
  "CampingSpot",
  "RockFormation",
  "RailwayTunnel",
  "FortressRuins",
  "Other",
] as const;
export type SpotCategory = (typeof SPOT_CATEGORIES)[number];

export const SPOT_MODERATION_STATUSES = ["Pending", "Approved", "Rejected"] as const;

export type SpotModerationStatus = (typeof SPOT_MODERATION_STATUSES)[number];

export interface NearbySpot {
  id: string;
  name: string;
  description: string;
  category: SpotCategory;
  photoUrl: string;
  latitude: number;
  longitude: number;
  createdByUserId: string;
  createdAt: string;
  distanceKm: number;
}

export interface SpotSearchResult {
  id: string;
  name: string;
  description: string;
  category: SpotCategory;
  photoUrl: string;
  latitude: number;
  longitude: number;
  createdByUserId: string;
  createdAt: string;
  moderationStatus: SpotModerationStatus;
  rejectionReason: string | null;
  scheduledDeletionAt: string | null;
}

export interface SpotResponse {
  id: string;
  name: string;
  description: string;
  category: SpotCategory;
  photoUrls: string[];
  latitude: number;
  longitude: number;
  createdByUserId: string;
  createdByDisplayName: string;
  averageRating: number;
  ratingsCount: number;
  createdAt: string;
  moderationStatus: SpotModerationStatus;
  rejectionReason: string | null;
  scheduledDeletionAt: string | null;
}

export interface CreateSpotCommand {
  name: string;
  description: string;
  category: SpotCategory;
  photoUrls: string[];
  latitude: number;
  longitude: number;
}

export interface UpdateSpotCommand {
  name: string;
  description: string;
  category: SpotCategory;
  photoUrls: string[];
}

export interface NearbySpotsResponse {
  items: NearbySpot[];
  totalCount: number;
}

export function getNearbySpots(
  lat: number,
  lng: number,
  radiusKm: number,
  category?: SpotCategory,
  signal?: AbortSignal,
): Promise<NearbySpotsResponse> {
  const params = new URLSearchParams({
    lat: String(lat),
    lng: String(lng),
    radiusKm: String(radiusKm),
  });
  if (category) params.set("category", category);
  return apiFetch<NearbySpotsResponse>(`/spots/nearby?${params.toString()}`, { signal });
}

export function createSpot(command: CreateSpotCommand): Promise<SpotResponse> {
  return apiFetch<SpotResponse>("/spots/", {
    method: "POST",
    body: JSON.stringify(command),
  });
}

export type SpotChangeRequestType = "Edit" | "Deletion";

export type SpotChangeRequestStatus = "Pending" | "Approved" | "Rejected";

export interface SpotChangeRequest {
  id: string;
  spotId: string;
  type: SpotChangeRequestType;
  status: SpotChangeRequestStatus;
  proposedName: string | null;
  proposedDescription: string | null;
  proposedCategory: SpotCategory | null;
  proposedPhotoUrls: string[] | null;
  rejectionReason: string | null;
  createdAt: string;
  reviewedAt: string | null;
}

// Doesn't change the spot — submits an edit an admin has to approve.
export function updateSpot(id: string, command: UpdateSpotCommand): Promise<SpotChangeRequest> {
  return apiFetch<SpotChangeRequest>(`/spots/${id}`, {
    method: "PUT",
    body: JSON.stringify(command),
  });
}

// Doesn't delete the spot — submits a deletion request an admin has to approve.
export function deleteSpot(id: string): Promise<SpotChangeRequest> {
  return apiFetch<SpotChangeRequest>(`/spots/${id}`, { method: "DELETE" });
}

// The creator's latest change request for the spot; 404 when there has never been one.
export function getMySpotChangeRequest(spotId: string, signal?: AbortSignal): Promise<SpotChangeRequest> {
  return apiFetch<SpotChangeRequest>(`/spots/${spotId}/change-request`, { signal });
}

export function cancelSpotChangeRequest(spotId: string): Promise<void> {
  return apiFetchVoid(`/spots/${spotId}/change-request`, { method: "DELETE" });
}

export function getSpot(id: string, signal?: AbortSignal): Promise<SpotResponse> {
  return apiFetch<SpotResponse>(`/spots/${id}`, { signal });
}

export interface SpotSearchPageResponse {
  items: SpotSearchResult[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function searchSpots(
  params: {q?: string; category?: SpotCategory; page?: number; pageSize?: number},
  signal?: AbortSignal,
) : Promise<SpotSearchPageResponse> {
  const searchParams = new URLSearchParams();
  if(params.q) searchParams.set("q", params.q);
  if(params.category) searchParams.set("category", params.category);
  if(params.page) searchParams.set("page", String(params.page));
  if(params.pageSize) searchParams.set("pageSize", String(params.pageSize));

  return apiFetch<SpotSearchPageResponse>(`/spots/search?${searchParams.toString()}`, {signal});
}

export function getMySpots(page: number, pageSize: number, signal?: AbortSignal): Promise<SpotSearchPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<SpotSearchPageResponse>(`/spots/mine?${params.toString()}`, { signal });
}
