import { apiFetch, apiFetchVoid, type PagedResponse } from "@/lib/apiClient";
import type { SpotCategory } from "@/lib/spotsApi";

export interface SavedSpotResponse {
  spotId: string;
  spotName: string;
  photoUrl: string;
  category: SpotCategory;
  savedAt: string;
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

export function fetchMySavedSpots(page: number, pageSize: number): Promise<PagedResponse<SavedSpotResponse>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<PagedResponse<SavedSpotResponse>>(`/saved-spots/me?${params.toString()}`);
}
