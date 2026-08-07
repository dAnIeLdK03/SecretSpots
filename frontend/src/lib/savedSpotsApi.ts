import { apiFetch, apiFetchVoid } from "@/lib/apiClient";
import type { SpotCategory } from "@/lib/spotsApi";

export interface SavedSpotResponse {
  spotId: string;
  spotName: string;
  photoUrl: string;
  category: SpotCategory;
  savedAt: string;
}

export interface SavedSpotsPageResponse {
  items: SavedSpotResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function saveSpot(spotId: string): Promise<void> {
  return apiFetchVoid(`/spots/${spotId}/saved`, { method: "PUT" });
}

export function unsaveSpot(spotId: string): Promise<void> {
  return apiFetchVoid(`/spots/${spotId}/saved`, { method: "DELETE" });
}

export function fetchIsSpotSaved(spotId: string, signal?: AbortSignal): Promise<boolean> {
  return apiFetch<{ saved: boolean }>(`/spots/${spotId}/saved/me`, { signal }).then((r) => r.saved);
}

export function fetchMySavedSpots(page: number, pageSize: number): Promise<SavedSpotsPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<SavedSpotsPageResponse>(`/saved-spots/me?${params.toString()}`);
}
