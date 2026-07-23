import { apiFetch, apiFetchVoid } from "@/lib/apiClient";

export const SPOT_CATEGORIES = ["Nature", "Viewpoint", "Cafe", "Abandoned"] as const;
export type SpotCategory = (typeof SPOT_CATEGORIES)[number];

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

export interface SpotResponse {
  id: string;
  name: string;
  description: string;
  category: SpotCategory;
  photoUrl: string;
  latitude: number;
  longitude: number;
  createdByUserId: string;
  createdAt: string;
}

export interface CreateSpotCommand {
  name: string;
  description: string;
  category: SpotCategory;
  photoUrl: string;
  latitude: number;
  longitude: number;
}

export interface UpdateSpotCommand {
  name: string;
  description: string;
  category: SpotCategory;
  photoUrl: string;
}

export function getNearbySpots(
  lat: number,
  lng: number,
  radiusKm: number,
  signal?: AbortSignal,
): Promise<NearbySpot[]> {
  const params = new URLSearchParams({
    lat: String(lat),
    lng: String(lng),
    radiusKm: String(radiusKm),
  });
  return apiFetch<NearbySpot[]>(`/spots/nearby?${params.toString()}`, { signal });
}

export function createSpot(command: CreateSpotCommand): Promise<SpotResponse> {
  return apiFetch<SpotResponse>("/spots/", {
    method: "POST",
    body: JSON.stringify(command),
  });
}

export function updateSpot(id: string, command: UpdateSpotCommand): Promise<SpotResponse> {
  return apiFetch<SpotResponse>(`/spots/${id}`, {
    method: "PUT",
    body: JSON.stringify(command),
  });
}

export function deleteSpot(id: string): Promise<void> {
  return apiFetchVoid(`/spots/${id}`, { method: "DELETE" });
}

export function getSpot(id: string, signal?: AbortSignal): Promise<SpotResponse> {
  return apiFetch<SpotResponse>(`/spots/${id}`, { signal });
}
