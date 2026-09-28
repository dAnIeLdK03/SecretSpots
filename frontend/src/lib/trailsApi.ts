import { apiFetch, apiFetchVoid } from "@/lib/apiClient";

export interface TrailPoint {
  latitude: number;
  longitude: number;
}

export interface NearbyTrail {
  id: string;
  name: string;
  description: string;
  photoUrl: string;
  points: TrailPoint[];
  distanceMeters: number;
  createdByUserId: string;
  createdAt: string;
}

export interface TrailResponse {
  id: string;
  name: string;
  description: string;
  photoUrls: string[];
  points: TrailPoint[];
  distanceMeters: number;
  createdByUserId: string;
  createdByDisplayName: string;
  createdAt: string;
}

export interface CreateTrailCommand {
  name: string;
  description: string;
  photoUrls: string[];
  points: TrailPoint[];
}

export interface NearbyTrailsResponse {
  items: NearbyTrail[];
  totalCount: number;
}

export function getNearbyTrails(lat: number, lng: number, radiusKm: number, signal?: AbortSignal): Promise<NearbyTrailsResponse> {
  const params = new URLSearchParams({ lat: String(lat), lng: String(lng), radiusKm: String(radiusKm) });
  return apiFetch<NearbyTrailsResponse>(`/trails/nearby?${params.toString()}`, { signal });
}

export function createTrail(command: CreateTrailCommand): Promise<TrailResponse> {
  return apiFetch<TrailResponse>("/trails/", {
    method: "POST",
    body: JSON.stringify(command),
  });
}

export function deleteTrail(id: string): Promise<void> {
  return apiFetchVoid(`/trails/${id}`, { method: "DELETE" });
}
